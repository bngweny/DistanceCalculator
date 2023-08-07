KdTreeNode BuildKdTree(List<VehicleData> vehicles, int depth = 0)
{
    if (vehicles.Count == 0)
    {
        return null;
    }

    int splittingDimension = depth % 2;
    vehicles.Sort((a, b) => splittingDimension == 0
        ? a.Latitude.CompareTo(b.Latitude)
        : a.Longitude.CompareTo(b.Longitude));

    int medianIndex = vehicles.Count / 2;
    var medianVehicle = vehicles[medianIndex];

    return new KdTreeNode
    {
        Vehicle = medianVehicle,
        SplittingDimension = splittingDimension,
        Left = BuildKdTree(vehicles.GetRange(0, medianIndex), depth + 1),
        Right = BuildKdTree(vehicles.GetRange(medianIndex + 1, vehicles.Count - medianIndex - 1), depth + 1)
    };
}

double CalculateHaversineDistance(float lat1, float lon1, float lat2, float lon2)
{
    const double EarthRadius = 6371; // in kilometers

    double latRad1 = lat1 * (Math.PI / 180);
    double lonRad1 = lon1 * (Math.PI / 180);
    double latRad2 = lat2 * (Math.PI / 180);
    double lonRad2 = lon2 * (Math.PI / 180);

    double dLat = latRad2 - latRad1;
    double dLon = lonRad2 - lonRad1;

    double a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(latRad1) * Math.Cos(latRad2) * Math.Pow(Math.Sin(dLon / 2), 2);
    double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

    return EarthRadius * c;
}

VehicleData FindNearestNeighbor(KdTreeNode node, (float Latitude, float Longitude) coordinate, VehicleData currentBest, double currentBestDistance, int depth = 0)
{
    if (node == null)
        return currentBest;

    double distance = CalculateHaversineDistance(coordinate.Latitude, coordinate.Longitude, node.Vehicle.Latitude, node.Vehicle.Longitude);

    if (currentBest == null || distance < currentBestDistance)
    {
        currentBest = node.Vehicle;
        currentBestDistance = distance;
    }

    int splittingDimension = depth % 2;

    if ((splittingDimension == 0 && coordinate.Latitude < node.Vehicle.Latitude) ||
        (splittingDimension == 1 && coordinate.Longitude < node.Vehicle.Longitude))
    {
        currentBest = FindNearestNeighbor(node.Left, coordinate, currentBest, currentBestDistance, depth + 1);
        if (splittingDimension == 0)
        {
            double distanceToSplittingPlane = Math.Abs(node.Vehicle.Latitude - coordinate.Latitude);
            if (distanceToSplittingPlane < currentBestDistance)
            {
                currentBest = FindNearestNeighbor(node.Right, coordinate, currentBest, currentBestDistance, depth + 1);
            }
        }
        else
        {
            double distanceToSplittingPlane = Math.Abs(node.Vehicle.Longitude - coordinate.Longitude);
            if (distanceToSplittingPlane < currentBestDistance)
            {
                currentBest = FindNearestNeighbor(node.Right, coordinate, currentBest, currentBestDistance, depth + 1);
            }
        }
    }
    else
    {
        currentBest = FindNearestNeighbor(node.Right, coordinate, currentBest, currentBestDistance, depth + 1);
        if (splittingDimension == 0)
        {
            double distanceToSplittingPlane = Math.Abs(node.Vehicle.Latitude - coordinate.Latitude);
            if (distanceToSplittingPlane < currentBestDistance)
            {
                currentBest = FindNearestNeighbor(node.Left, coordinate, currentBest, currentBestDistance, depth + 1);
            }
        }
        else
        {
            double distanceToSplittingPlane = Math.Abs(node.Vehicle.Longitude - coordinate.Longitude);
            if (distanceToSplittingPlane < currentBestDistance)
            {
                currentBest = FindNearestNeighbor(node.Left, coordinate, currentBest, currentBestDistance, depth + 1);
            }
        }
    }

    return currentBest;
}


string ReadNullTerminatedAsciiString(BinaryReader reader)
{
    List<byte> bytes = new List<byte>();
    byte currentByte;

    while ((currentByte = reader.ReadByte()) != 0)
    {
        bytes.Add(currentByte);
    }

    return System.Text.Encoding.ASCII.GetString(bytes.ToArray());
}

// There are more efficient file reading algorithms e,g using memory mapped files but since this application is to find
// an efficient spacial search algorithm I opted for to reduce complexity.
List<VehicleData> ReadVehicleDataFromDatFile(string filePath)
{
    List<VehicleData> vehicleDataList = new List<VehicleData>();

    using (FileStream fs = new FileStream(filePath, FileMode.Open))
    using (BinaryReader reader = new BinaryReader(fs))
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            VehicleData vehicleData = new VehicleData
            {
                VehicleId = reader.ReadInt32(),
                VehicleRegistration = ReadNullTerminatedAsciiString(reader),
                Latitude = reader.ReadSingle(),
                Longitude = reader.ReadSingle(),
                RecordedTimeUTC = reader.ReadUInt64()
            };

            vehicleDataList.Add(vehicleData);
        }
    }

    return vehicleDataList;
}


List<VehicleData> vehicleRecords = ReadVehicleDataFromDatFile(args.Length == 2 ? args[1] : "C:\\Users\\brandon.ngwenya\\Documents\\MiXTelematics\\Recruitment\\VehiclePositions\\VehiclePositions.dat");

var watch = new System.Diagnostics.Stopwatch();

watch.Start();
KdTreeNode root = BuildKdTree(vehicleRecords);
List<(float Latitude, float Longitude)> coordinates = new List<(float, float)>
{
    (34.544909f, -102.100843f),
    (32.345544f, -99.123124f),
    (33.234235f, -100.214124f),
    (35.195739f, -95.348899f),
    (31.895839f, -97.789573f),
    (32.895839f, -101.789573f),
    (34.115839f, -100.225732f),
    (32.335839f, -99.992232f),
    (33.535339f, -94.792232f),
    (32.234235f, -100.222222f)
};

foreach (var coordinate in coordinates)
{
    var closestVehicle = FindNearestNeighbor(root, coordinate, null, double.MaxValue);
    Console.WriteLine($"Closest vehicle to ({coordinate.Latitude}, {coordinate.Longitude}): VehicleId {closestVehicle.VehicleId}");
}
watch.Stop();
Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");

public class VehicleData
{
    public int VehicleId { get; set; }
    public string VehicleRegistration { get; set; }
    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public ulong RecordedTimeUTC { get; set; }
}

public class KdTreeNode
{
    public VehicleData Vehicle { get; set; }
    public int SplittingDimension { get; set; } // 0 for latitude, 1 for longitude
    public KdTreeNode Left { get; set; }
    public KdTreeNode Right { get; set; }
}

