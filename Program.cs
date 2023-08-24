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

Dictionary<(int, int), List<VehicleData>> CreateSpatialHashGrid(List<VehicleData> vehicleDataList, double cellSize)
{
    Dictionary<(int, int), List<VehicleData>> spatialHashGrid = new Dictionary<(int, int), List<VehicleData>>();

    foreach (var vehicle in vehicleDataList)
    {
        int cellX = (int)(vehicle.Latitude / cellSize);
        int cellY = (int)(vehicle.Longitude / cellSize);

        var cellKey = (cellX, cellY);
        if (!spatialHashGrid.ContainsKey(cellKey))
        {
            spatialHashGrid[cellKey] = new List<VehicleData>();
        }
        spatialHashGrid[cellKey].Add(vehicle);
    }

    return spatialHashGrid;
}

var watch = new System.Diagnostics.Stopwatch();

watch.Start();

List<VehicleData> vehicleRecords = ReadVehicleDataFromDatFile(args.Length == 1 ? args[0] : "VehiclePositions.dat");


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

double cellSize = 0.1;
Dictionary<(int, int), List<VehicleData>> spatialHashGrid = CreateSpatialHashGrid(vehicleRecords, cellSize);

foreach (var coordinate in coordinates)
{
    int targetCellX = (int)(coordinate.Latitude / cellSize);
    int targetCellY = (int)(coordinate.Longitude / cellSize);

    VehicleData closestVehicle = null;
    double closestDistance = double.MaxValue;

    for (int dx = -1; dx <= 1; dx++)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            var cellKey = (targetCellX + dx, targetCellY + dy);
            if (spatialHashGrid.TryGetValue(cellKey, out var vehiclesInCell))
            {
                foreach (var vehicle in vehiclesInCell)
                {
                    double distance = CalculateHaversineDistance(coordinate.Latitude, coordinate.Longitude, vehicle.Latitude, vehicle.Longitude);
                    if (distance <= closestDistance)
                    {
                        closestDistance = distance;
                        closestVehicle = vehicle;
                    }
                }
            }
        }
    }

    if (closestVehicle != null)
    {
        Console.WriteLine($"Closest vehicle to ({coordinate.Latitude}, {coordinate.Longitude}): VehicleId {closestVehicle.VehicleId}");
    }
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