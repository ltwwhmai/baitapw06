using System;
using System.Collections.Generic;
using System.Linq;


Console.OutputEncoding = System.Text.Encoding.UTF8;
DateTime borrowedDate = new DateTime(2026, 10, 19);

// Chạy một tình huống: bắt lỗi ở đây (lớp ứng dụng), rồi in trạng thái để chứng minh dữ liệu còn nhất quán
void Run(string title, Action action, Func<string>? showState = null)
{
    Console.WriteLine("--- " + title);
    try
    {
        action();
        Console.WriteLine("Không có lỗi");
    }
    catch (BorrowingException ex)
    {
        Console.WriteLine("Lỗi nghiệp vụ: " + ex.Message);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine("Lỗi tham số: " + ex.Message);
    }
    finally
    {
        if (showState != null) Console.WriteLine("Trạng thái sau thao tác: " + showState());
        Console.WriteLine();
    }
}

// ================= 11.1 LUỒNG THÀNH CÔNG =================
Console.WriteLine("===== LUỒNG THÀNH CÔNG =====");
try
{
    var lecturer = new Lecturer("GV01", "Nguyen Van A");
    var projector = new ProjectorRemote("MC01", "Điều khiển máy chiếu", 2024, 500000);
    var airCon = new AirConRemote("DH01", "Điều khiển điều hòa", 2023, 300000);
    var mic = new Microphone("MI01", "Micro không dây", 2022, 1000000);

    var room = new ClassRoom("P101", "Phòng 101", 50);
    room.AddDevice(projector);
    room.AddDevice(airCon);
    room.AddDevice(mic);
    Console.WriteLine("Chi phí bảo trì dự kiến/năm: " + room.CalculateAnnualMaintenanceCost());

    var slip = new BorrowingSlip("PM01", lecturer, "P101", borrowedDate);
    slip.AddDevice(projector, 2);
    slip.AddDevice(mic, 1);
    slip.AddDevice(airCon, 3);   // mượn nhưng cố ý KHÔNG trả, để có trạng thái "chưa trả"

    decimal fee1 = slip.ReturnDevice("MC01", borrowedDate.AddDays(2));   // đúng hạn
    decimal fee2 = slip.ReturnDevice("MI01", borrowedDate.AddDays(3));   // hạn là +1 ngày, trả +3 => muộn 2 ngày
    Console.WriteLine("Phí muộn máy chiếu: " + fee1);
    Console.WriteLine("Phí muộn micro: " + fee2);
    Console.WriteLine("Tổng phí muộn: " + slip.CalculateTotalLateFee());

    foreach (BorrowItem item in slip.Items)
    {
        string state = !item.IsReturned ? "CHƯA TRẢ"
                     : item.ReturnedDate > item.DueDate ? "TRẢ MUỘN"
                     : "trả đúng hạn";
        Console.WriteLine($"{item.Device.Name}: {state}");
    }
}
catch (BorrowingException ex)
{
    Console.WriteLine("Lỗi: " + ex.Message);
}
finally
{
    Console.WriteLine("Kết thúc luồng thành công\n");
}

// ================= 11.2 LUỒNG NGOẠI LỆ =================
Console.WriteLine("===== LUỒNG NGOẠI LỆ =====");

{   // 1
    var airCon = new AirConRemote("DH02", "Điều hòa", 2023, 300000);
    airCon.SendToMaintenance();
    var slip = new BorrowingSlip("PM02", new Lecturer("GV01", "A"), "P101", borrowedDate);
    Run("1. Mượn điều khiển điều hòa đang UnderMaintenance",
        () => slip.AddDevice(airCon, 1),
        () => $"điều hòa = {airCon.Status}, số mục trong phiếu = {slip.Items.Count}");
}

{   // 2
    var slip = new BorrowingSlip("PM03", new TeachingAssistant("TG01", "B"), "P101", borrowedDate);
    var mic3 = new Microphone("MI12", "Micro 3", 2023, 1000000);
    Run("2. Trợ giảng mượn thiết bị thứ ba",
        () =>
        {
            slip.AddDevice(new Microphone("MI10", "Micro 1", 2023, 1000000), 1);
            slip.AddDevice(new Microphone("MI11", "Micro 2", 2023, 1000000), 1);
            slip.AddDevice(mic3, 1);
        },
        () => $"micro 3 = {mic3.Status}, số mục trong phiếu = {slip.Items.Count}");
}

{   // 3
    var mic = new Microphone("MI20", "Micro", 2023, 1000000);
    var slip = new BorrowingSlip("PM04", new Lecturer("GV01", "A"), "P101", borrowedDate);
    Run("3. Thêm cùng một micro hai lần",
        () =>
        {
            slip.AddDevice(mic, 1);
            slip.AddDevice(mic, 1);
        },
        () => $"micro = {mic.Status}, số mục trong phiếu = {slip.Items.Count}");
}

{   // 4
    var mic = new Microphone("MI30", "Micro", 2023, 1000000);
    var slip = new BorrowingSlip("PM05", new Lecturer("GV01", "A"), "P101", borrowedDate);
    Run("4. Mượn micro 3 ngày (giới hạn 1 ngày)",
        () => slip.AddDevice(mic, 3),
        () => $"micro = {mic.Status}, số mục trong phiếu = {slip.Items.Count}");
}

{   // 5
    var slip = new BorrowingSlip("PM06", new Lecturer("GV01", "A"), "P101", borrowedDate);
    Run("5. Trả điều khiển máy chiếu không thuộc phiếu",
        () => slip.ReturnDevice("MC99", borrowedDate.AddDays(1)));
}

{   // 6
    var mic = new Microphone("MI40", "Micro", 2023, 1000000);
    var slip = new BorrowingSlip("PM07", new Lecturer("GV01", "A"), "P101", borrowedDate);
    Run("6. Trả cùng một thiết bị hai lần",
        () =>
        {
            slip.AddDevice(mic, 1);
            slip.ReturnDevice("MI40", borrowedDate.AddDays(1));
            slip.ReturnDevice("MI40", borrowedDate.AddDays(1));
        },
        () => $"micro = {mic.Status}");
}

{   // 7
    var mic = new Microphone("MI50", "Micro", 2023, 1000000);
    var slip = new BorrowingSlip("PM08", new Lecturer("GV01", "A"), "P101", borrowedDate);
    slip.AddDevice(mic, 1);
    Run("7. Trả thiết bị với ngày trả trước ngày mượn",
        () => slip.ReturnDevice("MI50", borrowedDate.AddDays(-1)),
        () => $"micro = {mic.Status} (vẫn phải là Borrowed), đã trả = {slip.Items[0].IsReturned}");
}

// ================= TEST BỔ SUNG =================
Console.WriteLine("===== TEST BỔ SUNG =====");

{   // số ngày mượn <= 0
    var mic = new Microphone("MI60", "Micro", 2023, 1000000);
    var slip = new BorrowingSlip("PM09", new Lecturer("GV01", "A"), "P101", borrowedDate);
    Run("8. Mượn với số ngày = 0",
        () => slip.AddDevice(mic, 0),
        () => $"micro = {mic.Status}, số mục = {slip.Items.Count}");
}

{   // tiết mượn sai
    var mic = new Microphone("MI70", "Micro", 2023, 1000000);
    var slip = new BorrowingSlip("PM10", new Lecturer("GV01", "A"), "P101", borrowedDate);
    Run("9. Tiết mượn sai (từ tiết 5 đến tiết 2)",
        () => slip.AddDevice(mic, 1, 5, 2),
        () => $"micro = {mic.Status} (vẫn phải là Available), số mục = {slip.Items.Count}");
}

{   // ClassRoom
    var room = new ClassRoom("P102", "Phòng 102", 40);
    var oldMic = new Microphone("MI80", "Micro cũ", 2020, 1000000);   // dùng trên 5 năm
    var airCon = new AirConRemote("DH80", "Điều hòa", 2025, 300000);
    airCon.SendToMaintenance();
    room.AddDevice(oldMic);
    room.AddDevice(airCon);

    Run("10. Thêm thiết bị trùng mã vào phòng", () => room.AddDevice(oldMic));
    Run("11. Thêm thiết bị null vào phòng", () => room.AddDevice(null!));
    Run("12. Thiết bị cần bảo trì và xóa thiết bị",
        () =>
        {
            Console.WriteLine("Cần bảo trì: " + string.Join("; ", room.GetDevicesRequiringMaintenance().Select(d => d.DeviceId)));
            Console.WriteLine("Xóa DH80: " + room.RemoveDevice("DH80"));
            Console.WriteLine("Xóa DH80 lần 2: " + room.RemoveDevice("DH80"));
            Console.WriteLine("Tìm DH80: " + (room.FindDevice("DH80") == null ? "không thấy" : "thấy"));
        });
}

// Constructor Device
Run("13. Mã thiết bị rỗng", () => new Microphone("", "Micro", 2023, 1000000));
Run("14. Giá mua <= 0", () => new Microphone("MI90", "Micro", 2023, 0));
Run("15. Năm sử dụng ở tương lai", () => new Microphone("MI91", "Micro", 2999, 1000000));

// ===== Enums/DeviceStatus.cs =====
public enum DeviceStatus
{
    Available,
    Borrowed,
    UnderMaintenance
}

// ===== Exceptions/BorrowingException.cs =====
public class BorrowingException : Exception
{
    public BorrowingException(string message) : base(message) { }

    public BorrowingException(string message, Exception innerException)
        : base(message, innerException) { }
}

// ===== Exceptions/DeviceUnavailableException.cs =====
public class DeviceUnavailableException : BorrowingException
{
    public string DeviceId { get; }
    public DeviceStatus CurrentStatus { get; }

    public DeviceUnavailableException(string deviceId, DeviceStatus currentStatus)
        : base($"Thiết bị {deviceId} không sẵn sàng, trạng thái: {currentStatus}")
    {
        DeviceId = deviceId;
        CurrentStatus = currentStatus;
    }
}

// ===== Exceptions/BorrowLimitExceededException.cs =====
public class BorrowLimitExceededException : BorrowingException
{
    public string BorrowerId { get; }
    public int MaximumDevices { get; }

    public BorrowLimitExceededException(string borrowerId, int maximumDevices)
        : base($"Người mượn {borrowerId} đã đạt tối đa {maximumDevices} thiết bị")
    {
        BorrowerId = borrowerId;
        MaximumDevices = maximumDevices;
    }
}

// ===== Exceptions/InvalidBorrowPeriodException.cs =====
public class InvalidBorrowPeriodException : BorrowingException
{
    public string DeviceId { get; }
    public int RequestedDays { get; }
    public int MaximumDays { get; }

    public InvalidBorrowPeriodException(string deviceId, int requestedDays, int maximumDays)
        : base($"Số ngày mượn {requestedDays} không hợp lệ cho thiết bị {deviceId} (tối đa {maximumDays})")
    {
        DeviceId = deviceId;
        RequestedDays = requestedDays;
        MaximumDays = maximumDays;
    }
}

// ===== Exceptions/DuplicateDeviceException.cs =====
public class DuplicateDeviceException : BorrowingException
{
    public DuplicateDeviceException(string deviceId)
        : base($"Thiết bị {deviceId} đã tồn tại") { }
}

// ===== Exceptions/DeviceNotInSlipException.cs =====
public class DeviceNotInSlipException : BorrowingException
{
    public DeviceNotInSlipException(string deviceId)
        : base($"Thiết bị {deviceId} không thuộc phiếu mượn") { }
}

// ===== Exceptions/DeviceAlreadyReturnedException.cs =====
public class DeviceAlreadyReturnedException : BorrowingException
{
    public DeviceAlreadyReturnedException(string deviceId)
        : base($"Thiết bị {deviceId} đã được trả trước đó") { }
}

// ===== Exceptions/InvalidReturnDateException.cs =====
// Ngoại lệ bổ sung: đề liệt kê "ngày trả không hợp lệ" nhưng không có lớp riêng
public class InvalidReturnDateException : BorrowingException
{
    public InvalidReturnDateException(string message) : base(message) { }
}

// ===== Devices/Device.cs =====
public abstract class Device
{
    public string DeviceId { get; }            // chỉ gán lúc khởi tạo
    public string Name { get; }
    public int YearInUse { get; }
    public decimal Price { get; }
    public DeviceStatus Status { get; protected set; }   // bên ngoài không gán trực tiếp được

    protected Device(string deviceId, string name, int yearInUse, decimal price)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Mã thiết bị không được rỗng");
        if (price <= 0)
            throw new ArgumentException("Giá mua phải lớn hơn 0");
        if (yearInUse > DateTime.Now.Year)
            throw new ArgumentException("Năm sử dụng không được lớn hơn năm hiện tại");

        DeviceId = deviceId;
        Name = name;
        YearInUse = yearInUse;
        Price = price;
        Status = DeviceStatus.Available;
    }

    public void SendToMaintenance()
    {
        if (Status == DeviceStatus.Borrowed)
            throw new InvalidOperationException("Thiết bị đang được mượn, chưa thể bảo trì");
        Status = DeviceStatus.UnderMaintenance;
    }

    public abstract decimal CalculateMaintenanceCost();

    public override string ToString()
    {
        return $"{DeviceId} | {Name} | {YearInUse} | {Price} | {Status}";
    }
}

// ===== Devices/TeachingDevice.cs =====
public abstract class TeachingDevice : Device
{
    public int MaxBorrowDays { get; }
    public decimal LateFeePerDay { get; }

    protected TeachingDevice(string deviceId, string name, int yearInUse, decimal price,
                             int maxBorrowDays, decimal lateFeePerDay)
        : base(deviceId, name, yearInUse, price)
    {
        MaxBorrowDays = maxBorrowDays;
        LateFeePerDay = lateFeePerDay;
    }

    public void MarkAsBorrowed()
    {
        // chốt chặn cuối: chỉ thiết bị Available mới được chuyển sang Borrowed
        if (Status != DeviceStatus.Available)
            throw new DeviceUnavailableException(DeviceId, Status);
        Status = DeviceStatus.Borrowed;
    }

    public void MarkAsAvailable()
    {
        Status = DeviceStatus.Available;
    }
}

// ===== Devices/AirConRemote.cs =====
public class AirConRemote : TeachingDevice
{
    private const int MaxDays = 3;
    private const decimal LateFee = 20000;
    private const decimal CostRate = 0.05m;

    public AirConRemote(string deviceId, string name, int yearInUse, decimal price)
        : base(deviceId, name, yearInUse, price, MaxDays, LateFee) { }

    public override decimal CalculateMaintenanceCost() => Price * CostRate;
}

// ===== Devices/Microphone.cs =====
public class Microphone : TeachingDevice
{
    private const int MaxDays = 1;
    private const decimal LateFee = 10000;
    private const decimal CostRate = 0.10m;

    public Microphone(string deviceId, string name, int yearInUse, decimal price)
        : base(deviceId, name, yearInUse, price, MaxDays, LateFee) { }

    public override decimal CalculateMaintenanceCost() => Price * CostRate;
}

// ===== Devices/ProjectorRemote.cs =====
public class ProjectorRemote : TeachingDevice
{
    private const int MaxDays = 3;
    private const decimal LateFee = 30000;
    private const decimal CostRate = 0.08m;
    private const decimal FixedCost = 50000;

    public ProjectorRemote(string deviceId, string name, int yearInUse, decimal price)
        : base(deviceId, name, yearInUse, price, MaxDays, LateFee) { }

    public override decimal CalculateMaintenanceCost() => Price * CostRate + FixedCost;
}

// ===== Borrowers/IBorrower.cs =====
public interface IBorrower
{
    string BorrowerId { get; }
    string FullName { get; }
    int GetMaximumDevices();
}

// ===== Borrowers/Borrower.cs =====
public abstract class Borrower : IBorrower
{
    public string BorrowerId { get; }
    public string FullName { get; }

    protected Borrower(string borrowerId, string fullName)
    {
        if (string.IsNullOrWhiteSpace(borrowerId))
            throw new ArgumentException("Mã người mượn không được rỗng");
        BorrowerId = borrowerId;
        FullName = fullName;
    }

    public abstract int GetMaximumDevices();
}

// ===== Borrowers/Lecturer.cs =====
public class Lecturer : Borrower
{
    private const int MaxDevices = 3;

    public Lecturer(string borrowerId, string fullName) : base(borrowerId, fullName) { }

    public override int GetMaximumDevices() => MaxDevices;
}

// ===== Borrowers/TeachingAssistant.cs =====
public class TeachingAssistant : Borrower
{
    private const int MaxDevices = 2;

    public TeachingAssistant(string borrowerId, string fullName) : base(borrowerId, fullName) { }

    public override int GetMaximumDevices() => MaxDevices;
}

// ===== Models/ClassRoom.cs =====
public class ClassRoom
{
    public string RoomId { get; }
    public string RoomName { get; }
    public int Capacity { get; }
    private readonly List<Device> _devices = new();

    public ClassRoom(string roomId, string roomName, int capacity)
    {
        RoomId = roomId;
        RoomName = roomName;
        Capacity = capacity;
    }

    public void AddDevice(Device device)
    {
        if (device == null)
            throw new ArgumentNullException(nameof(device));
        if (FindDevice(device.DeviceId) != null)
            throw new DuplicateDeviceException(device.DeviceId);
        _devices.Add(device);
    }

    public bool RemoveDevice(string deviceId)
    {
        Device? device = FindDevice(deviceId);
        if (device == null) return false;
        return _devices.Remove(device);
    }

    public Device? FindDevice(string deviceId)
    {
        return _devices.FirstOrDefault(d => d.DeviceId == deviceId);
    }

    public decimal CalculateAnnualMaintenanceCost()
    {
        return _devices.Sum(d => d.CalculateMaintenanceCost());
    }

    public List<Device> GetDevicesRequiringMaintenance()
    {
        return _devices
            .Where(d => d.Status == DeviceStatus.UnderMaintenance
                     || DateTime.Now.Year - d.YearInUse > 5)
            .ToList();
    }
}

// ===== Models/BorrowItem.cs =====
public class BorrowItem
{
    public TeachingDevice Device { get; }
    public DateTime BorrowedDate { get; }
    public DateTime DueDate { get; }
    public int FromPeriod { get; }
    public int ToPeriod { get; }
    public DateTime? ReturnedDate { get; private set; }
    public bool IsReturned => ReturnedDate != null;

    public BorrowItem(TeachingDevice device, DateTime borrowedDate, DateTime dueDate,
                      int fromPeriod = 1, int toPeriod = 1)
    {
        if (device == null)
            throw new ArgumentNullException(nameof(device));
        if (dueDate < borrowedDate)
            throw new ArgumentException("Hạn trả phải sau hoặc bằng ngày mượn");
        if (fromPeriod < 1 || toPeriod < fromPeriod)
            throw new ArgumentException("Tiết kết thúc phải lớn hơn hoặc bằng tiết bắt đầu (và tiết bắt đầu >= 1)");

        Device = device;
        BorrowedDate = borrowedDate;
        DueDate = dueDate;
        FromPeriod = fromPeriod;
        ToPeriod = toPeriod;
        ReturnedDate = null;
    }

    public void Return(DateTime returnedDate)
    {
        if (IsReturned)
            throw new DeviceAlreadyReturnedException(Device.DeviceId);
        if (returnedDate < BorrowedDate)
            throw new InvalidReturnDateException("Ngày trả không được trước ngày mượn");
        ReturnedDate = returnedDate;
    }

    public decimal CalculateLateFee()
    {
        if (ReturnedDate == null) return 0;
        int lateDays = (ReturnedDate.Value.Date - DueDate.Date).Days;   
        return lateDays > 0 ? lateDays * Device.LateFeePerDay : 0;
    }
}

// ===== Models/BorrowingSlip.cs =====
public class BorrowingSlip
{
    public string SlipId { get; }
    public IBorrower Borrower { get; }
    public string TeachingRoom { get; }
    public DateTime BorrowedDate { get; }
    private readonly List<BorrowItem> _items = new();
    public IReadOnlyList<BorrowItem> Items => _items.AsReadOnly();

    public BorrowingSlip(string slipId, IBorrower borrower, string teachingRoom, DateTime borrowedDate)
    {
        if (string.IsNullOrWhiteSpace(slipId))
            throw new ArgumentException("Mã phiếu không hợp lệ");
        if (borrower == null)
            throw new ArgumentNullException(nameof(borrower));
        if (string.IsNullOrWhiteSpace(teachingRoom))
            throw new ArgumentException("Phòng dạy không hợp lệ");

        SlipId = slipId;
        Borrower = borrower;
        TeachingRoom = teachingRoom;
        BorrowedDate = borrowedDate;
    }

    private BorrowItem? FindItem(string deviceId)
    {
        return _items.FirstOrDefault(i => i.Device.DeviceId == deviceId);
    }

    private void ValidateBorrowing(TeachingDevice device, int borrowDays)
    {
        if (device == null)
            throw new ArgumentNullException(nameof(device));
        if (borrowDays <= 0 || borrowDays > device.MaxBorrowDays)
            throw new InvalidBorrowPeriodException(device.DeviceId, borrowDays, device.MaxBorrowDays);
        if (FindItem(device.DeviceId) != null)                       // trùng phải kiểm tra trước trạng thái
            throw new DuplicateDeviceException(device.DeviceId);
        if (_items.Count >= Borrower.GetMaximumDevices())
            throw new BorrowLimitExceededException(Borrower.BorrowerId, Borrower.GetMaximumDevices());
        if (device.Status != DeviceStatus.Available)
            throw new DeviceUnavailableException(device.DeviceId, device.Status);
    }

    public void AddDevice(TeachingDevice device, int borrowDays, int fromPeriod = 1, int toPeriod = 1)
    {
        ValidateBorrowing(device, borrowDays);

        var item = new BorrowItem(device, BorrowedDate, BorrowedDate.AddDays(borrowDays), fromPeriod, toPeriod);
        device.MarkAsBorrowed();
        _items.Add(item);
    }

    public decimal ReturnDevice(string deviceId, DateTime returnedDate)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Mã thiết bị không hợp lệ");

        BorrowItem item = FindItem(deviceId) ?? throw new DeviceNotInSlipException(deviceId);

        item.Return(returnedDate);       
        item.Device.MarkAsAvailable();
        return item.CalculateLateFee();
    }

    public decimal CalculateTotalLateFee()
    {
        return _items.Sum(i => i.CalculateLateFee());
    }
}
