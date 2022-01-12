using Fusee.Base.Core;
using Fusee.Engine.Core.Scene;
using Fusee.Math.Core;
using Fusee.PointCloud.Common;
using Fusee.PointCloud.Potree2ReaderWriter.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Fusee.PointCloud.Potree2ReaderWriter
{
    public class PtOctreePotree2FileReader
    {
        internal const string HierarchyFileName = "hierarchy.bin";
        internal const string MetadataFileName = "metadata.json";
        internal const string OctreeFileName = "octree.bin";

        public static bool CanHandleFile(string pathToNodeFileFolder, bool ignoreVersion = false)
        {
            var hierarchyFilePath = Path.Combine(pathToNodeFileFolder, HierarchyFileName);
            var metadataFilePath = Path.Combine(pathToNodeFileFolder, MetadataFileName);
            var octreeFilePath = Path.Combine(pathToNodeFileFolder, OctreeFileName);

            var canHandle = true;

            if (File.Exists(metadataFilePath))
            {
                try
                {
                    var metadata = JsonConvert.DeserializeObject<PotreeMetadata>(File.ReadAllText(metadataFilePath));

                    if (!ignoreVersion && metadata.Version != "2.0")
                    {
                        canHandle = false;
                        Diagnostics.Warn("File metadata indicates unsupported version. Metadata version: " + metadata.Version + ", supportet version: 2.0");
                    }

                    if (!metadata.Encoding.Contains("DEFAULT"))
                    {
                        canHandle = false;
                        Diagnostics.Warn("Non-default encoding is not supported!");
                    }
                }
                catch
                {
                    canHandle = false;
                    Diagnostics.Warn("Cannot parse " + metadataFilePath);
                }
            }
            else
            {
                canHandle = false;
                Diagnostics.Warn("File does not exist " + metadataFilePath);
            }

            if (!File.Exists(hierarchyFilePath))
            {
                canHandle = false;
                Diagnostics.Warn("File does not exist " + hierarchyFilePath);
            }

            if (!File.Exists(octreeFilePath))
            {
                canHandle = false;
                Diagnostics.Warn("File does not exist " + octreeFilePath);
            }

            return canHandle;
        }

        public static PointType GetPointType(string pathToNodeFileFolder = "")
        {
            return PointType.Position_double__Color_float__Label_byte;
        }
    }

    public class PtOctreePotree2FileReader<TPoint> : PtOctreePotree2FileReader, IPtOctreeFileReader
    {
        public string HierarchyFilePath { get => Path.Combine(FilePath, HierarchyFileName); }
        public string MetadataFilePath { get => Path.Combine(FilePath, MetadataFileName); }
        public string OctreeFilePath { get => Path.Combine(FilePath, OctreeFileName); }

        private BinaryReader binaryReader;

        public string FilePath { get; private set; }

        public PotreeMetadata Metadata { get; private set; }

        public PotreeHierarchy Hierarchy { get; private set; }

        /// <summary>
        /// Number of octants/nodes that are currently loaded.
        /// </summary>
        public int NumberOfOctants { get; private set; }


        public SceneNode GetScene()
        {
            throw new NotImplementedException();
        }

        public PtOctreePotree2FileReader(string pathToNodeFileFolder)
        {
            FilePath = pathToNodeFileFolder;

            if (CanHandleFile(FilePath))
            {
                Metadata = JsonConvert.DeserializeObject<PotreeMetadata>(File.ReadAllText(MetadataFilePath));
                Hierarchy = LoadHierarchy();

                binaryReader = new BinaryReader(File.OpenRead(OctreeFilePath));
            }
            else
            {
                throw new NotImplementedException(FilePath);
            }
        }

        private PotreeHierarchy LoadHierarchy()
        {
            var firstChunkSize = Metadata.Hierarchy.FirstChunkSize;
            var stepSize = Metadata.Hierarchy.StepSize;
            var depth = Metadata.Hierarchy.Depth;

            var data = File.ReadAllBytes(HierarchyFilePath);

            PotreeNode root = new PotreeNode()
            {
                Name = "r",
                Aabb = new AABBd(Metadata.BoundingBox.Min, Metadata.BoundingBox.Max)
            };

            var hierarchy = new PotreeHierarchy();

            long offset = 0;
            LoadHierarchyRecursive(ref root, ref data, offset, firstChunkSize);

            hierarchy.Nodes = new();
            root.Traverse(n => hierarchy.Nodes.Add(n));

            hierarchy.TreeRoot = root;

            return hierarchy;
        }

        private void LoadHierarchyRecursive(ref PotreeNode root, ref byte[] data, long offset, long size)
        {
            int bytesPerNode = 22;
            int numNodes = (int)(size / bytesPerNode);

            var nodes = new List<PotreeNode>(numNodes)
            {
                root
            };

            for (int i = 0; i < numNodes; i++)
            {
                var currentNode = nodes[i];
                if (currentNode == null)
                    currentNode = new PotreeNode();

                ulong offsetNode = (ulong)offset + (ulong)(i * bytesPerNode);

                var nodeType = data[offsetNode + 0];
                int childMask = BitConverter.ToInt32(data, (int)offsetNode + 1);
                var numPoints = BitConverter.ToUInt32(data, (int)offsetNode + 2);
                var byteOffset = BitConverter.ToInt64(data, (int)offsetNode + 6);
                var byteSize = BitConverter.ToInt64(data, (int)offsetNode + 14);

                currentNode.NodeType = (NodeType)nodeType;
                currentNode.NumPoints = numPoints;
                currentNode.ByteOffset = byteOffset;
                currentNode.ByteSize = byteSize;

                if (currentNode.NodeType == NodeType.PROXY)
                {
                    LoadHierarchyRecursive(ref currentNode, ref data, byteOffset, byteSize);
                }
                else
                {
                    for (int childIndex = 0; childIndex < 8; childIndex++)
                    {
                        bool childExists = ((1 << childIndex) & childMask) != 0;

                        if (!childExists)
                        {
                            continue;
                        }

                        string childName = currentNode.Name + childIndex.ToString();

                        PotreeNode child = new PotreeNode();

                        child.Aabb = childAABB(currentNode.Aabb, childIndex);
                        child.Name = childName;
                        currentNode.children[childIndex] = child;
                        child.Parent = currentNode;

                        nodes.Add(child);
                    }
                }
            }

            AABBd childAABB(AABBd aabb, int index)
            {

                double3 min = aabb.min;
                double3 max = aabb.max;

                double3 size = max - min;

                if ((index & 0b0001) > 0)
                {
                    min.z += size.z / 2;
                }
                else
                {
                    max.z -= size.z / 2;
                }

                if ((index & 0b0010) > 0)
                {
                    min.y += size.y / 2;
                }
                else
                {
                    max.y -= size.y / 2;
                }

                if ((index & 0b0100) > 0)
                {
                    min.x += size.x / 2;
                }
                else
                {
                    max.x -= size.x / 2;
                }

                return new AABBd(min, max);
            }
        }

        public PotreeNode FindNode(string id)
        {
            return Hierarchy.Nodes.Find(n => n.Name == id);
        }

        public Position_double__Color_float__Label_byte[] LoadNodeData(string id)
        {
            var node = FindNode(id);

            var points = new Position_double__Color_float__Label_byte[node.NumPoints];

            var attributeOffset = 0;

            foreach (var metaitem in Metadata.Attributes)
            {
                if (metaitem.Name == "position")
                {
                    for (int i = 0; i < node.NumPoints; i++)
                    {
                        binaryReader.BaseStream.Position = node.ByteOffset + attributeOffset + i * Metadata.PointSize;

                        points[i].Position.x = (binaryReader.ReadInt32() * (float)Metadata.Scale.x) + (float)Metadata.Offset.x;
                        points[i].Position.y = (binaryReader.ReadInt32() * (float)Metadata.Scale.y) + (float)Metadata.Offset.y;
                        points[i].Position.z = (binaryReader.ReadInt32() * (float)Metadata.Scale.z) + (float)Metadata.Offset.z;

                        // In js they subtract the min offset for every point,I guess that is just moving the pointcloud to the coordinate origin.
                        // We should do this in usercode
                    }
                }
                else if (metaitem.Name.Contains("rgb"))
                {
                    for (int i = 0; i < node.NumPoints; i++)
                    {
                        binaryReader.BaseStream.Position = node.ByteOffset + attributeOffset + i * Metadata.PointSize;

                        points[i].Color.r = binaryReader.ReadUInt16() / ushort.MaxValue;
                        points[i].Color.g = binaryReader.ReadUInt16() / ushort.MaxValue;
                        points[i].Color.b = binaryReader.ReadUInt16() / ushort.MaxValue;
                    }
                }
                else if (metaitem.Name.Equals("classification"))
                {
                    for (int i = 0; i < node.NumPoints; i++)
                    {
                        binaryReader.BaseStream.Position = node.ByteOffset + attributeOffset + i + Metadata.PointSize;

                        points[i].Label = (byte)binaryReader.ReadSByte();
                    }
                }

                attributeOffset += metaitem.Size;
            }

            node.IsLoaded = true;

            return points;
        }
    }
}
