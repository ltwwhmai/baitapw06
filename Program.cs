/****************/

// 202418940  

// Le Thanh Mai

/****************/  
using System;
using System.Collections.Generic;
using System.Linq;

Console.OutputEncoding = System.Text.Encoding.UTF8;
DateTime borrowedDate = new DateTime(2026, 10, 19);

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

// 11.1 LUỒNG THÀNH CÔNG 
Console.WriteLine("===== LUỒNG THÀNH CÔNG =====");
try
{
    // Tạo dữ liệu người mượn và các thiết bị
    var lecturer = new Lecturer("GV01", "Nguyen Van A");
    var projector = new ProjectorRemote("MC01", "Điều khiển máy chiếu", 2024, 500000);
    var airCon = new AirConRemote("DH01", "Điều khiển điều hòa", 2023, 300000);
    var mic = new Microphone("MI01", "Micro không dây", 2022, 1000000);

    // Xếp thiết bị vào phòng học và tính phí bảo trì
    var room = new ClassRoom("P101", "Phòng 101", 50);
    room.AddDevice(projector);
    room.AddDevice(airCon);
    room.AddDevice(mic);
    Console.WriteLine("Chi phí bảo trì dự kiến/năm: " + room.CalculateAnnualMaintenanceCost());

    // Tạo phiếu mượn thiết bị
    var slip = new BorrowingSlip("PM01", lecturer, "P101", borrowedDate);
    slip.AddDevice(projector, 2);
    slip.AddDevice(mic, 1);
    slip.AddDevice(airCon, 3);   

    // Trả thiết bị và tính phí muộn (nếu có)
    decimal fee1 = slip.ReturnDevice("MC01", borrowedDate.AddDays(2));   // Trả đúng hạn => không bị phạt
    decimal fee2 = slip.ReturnDevice("MI01", borrowedDate.AddDays(3));   // Hạn 1 ngày, mượn 3 ngày => muộn 2 ngày
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

// 11.2 LUỒNG NGOẠI LỆ
Console.WriteLine("===== LUỒNG NGOẠI LỆ =====");

{   // 1. Lỗi do thiết bị đang đi bảo trì, không thể mượn
    var airCon = new AirConRemote("DH02", "Điều hòa", 2023, 300000);
    airCon.SendToMaintenance();
    var slip = new BorrowingSlip("PM02", new Lecturer("GV01", "A"), "P101", borrowedDate);
    Run("1. Mượn điều khiển điều hòa đang UnderMaintenance",
        () => slip.AddDevice(airCon, 1),
        () => $"điều hòa = {airCon.Status}, số mục trong phiếu = {slip.Items.Count}");
}

{   // 2. Lỗi mượn vượt quá số lượng cho phép (Trợ giảng chỉ được mượn tối đa 2 thiết bị)
    var slip = new BorrowingSlip("PM03", new TeachingAssistant("TG01", "B"), "P101", borrowedDate);
    var mic3 = new Microphone("MI12", "Micro 3", 2023, 1000000);
    Run("2. Trợ giảng mượn thiết bị thứ ba",
        () =>
        {
            slip.AddDevice(new Microphone("MI10", "Micro 1", 2023, 1000000), 1);
            slip.AddDevice(new Microphone("MI11", "Micro 2", 2023, 1000000), 1);
            slip.AddDevice(mic3, 1); // Lỗi
        },
        () => $"micro 3 = {mic3.Status}, số mục trong phiếu = {slip.Items.Count}");
}

{   // 3. Lỗi mượn cùng 1 thiết bị 2 lần trong cùng 1 phiếu
    var mic = new Microphone("MI20", "Micro", 2023, 1000000);
    var slip = new BorrowingSlip("PM04", new Lecturer("GV01", "A"), "P101", borrowedDate);
    Run("3. Thêm cùng một micro hai lần",
        () =>
        {
            slip.AddDevice(mic, 1);
            slip.AddDevice(mic, 1); // Lỗi 
        },
        () => $"micro = {mic.Status}, số mục trong phiếu = {slip.Items.Count}");
}

{   // 4. Lỗi mượn vượt quá số ngày cho phép của thiết bị (Micro chỉ cho mượn 1 ngày)
    var mic = new Microphone("MI30", "Micro", 2023, 1000000);
    var slip = new BorrowingSlip("PM05", new Lecturer("GV01", "A"), "P101", borrowedDate);
    Run("4. Mượn micro 3 ngày (giới hạn 1 ngày)",
        () => slip.AddDevice(mic, 3),
        () => $"micro = {mic.Status}, số mục trong phiếu = {slip.Items.Count}");
}

{   // 5. Lỗi trả thiết bị không có trong phiếu mượn
    var slip = new BorrowingSlip("PM06", new Lecturer("GV01", "A"), "P101", borrowedDate);
    Run("5. Trả điều khiển máy chiếu không thuộc phiếu",
        () => slip.ReturnDevice("MC99", borrowedDate.AddDays(1)));
}

{   // 6. Lỗi trả 1 thiết bị 2 lần
    var mic = new Microphone("MI40", "Micro", 2023, 1000000);
    var slip = new BorrowingSlip("PM07", new Lecturer("GV01", "A"), "P101", borrowedDate);
    Run("6. Trả cùng một thiết bị hai lần",
        () =>
        {
            slip.AddDevice(mic, 1);
            slip.ReturnDevice("MI40", borrowedDate.AddDays(1));
            slip.ReturnDevice("MI40", borrowedDate.AddDays(1)); // Lỗi
        },
        () => $"micro = {mic.Status}");
}

{   // 7. Lỗi logic thời gian: Ngày trả lại diễn ra trước ngày mượn
    var mic = new Microphone("MI50", "Micro", 2023, 1000000);
    var slip = new BorrowingSlip("PM08", new Lecturer("GV01", "A"), "P101", borrowedDate);
    slip.AddDevice(mic, 1);
    Run("7. Trả thiết bị với ngày trả trước ngày mượn",
        () => slip.ReturnDevice("MI50", borrowedDate.AddDays(-1)),
        () => $"micro = {mic.Status} (vẫn phải là Borrowed), đã trả = {slip.Items[0].IsReturned}");
}

//  TEST BỔ SUNG CÁC LỖI LOGIC KHÁC 
Console.WriteLine("===== TEST BỔ SUNG =====");

{   // Lỗi nhập số ngày mượn <= 0
    var mic = new Microphone("MI60", "Micro", 2023, 1000000);
    var slip = new BorrowingSlip("PM09", new Lecturer("GV01", "A"), "P101", borrowedDate);
    Run("8. Mượn với số ngày = 0",
        () => slip.AddDevice(mic, 0),
        () => $"micro = {mic.Status}, số mục = {slip.Items.Count}");
}

{   // Lỗi tiết bắt đầu lớn hơn tiết kết thúc
    var mic = new Microphone("MI70", "Micro", 2023, 1000000);
    var slip = new BorrowingSlip("PM10", new Lecturer("GV01", "A"), "P101", borrowedDate);
    Run("9. Tiết mượn sai (từ tiết 5 đến tiết 2)",
        () => slip.AddDevice(mic, 1, 5, 2),
        () => $"micro = {mic.Status} (vẫn phải là Available), số mục = {slip.Items.Count}");
}

{   // Test các thao tác với Phòng học (ClassRoom)
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
            // Lọc ra các thiết bị cần bảo trì
            Console.WriteLine("Cần bảo trì: " + string.Join("; ", room.GetDevicesRequiringMaintenance().Select(d => d.DeviceId)));
            Console.WriteLine("Xóa DH80: " + room.RemoveDevice("DH80"));
            Console.WriteLine("Xóa DH80 lần 2: " + room.RemoveDevice("DH80"));
            Console.WriteLine("Tìm DH80: " + (room.FindDevice("DH80") == null ? "không thấy" : "thấy"));
        });
}

// Test tính hợp lệ khi tạo mới thiết bị
Run("13. Mã thiết bị rỗng", () => new Microphone("", "Micro", 2023, 1000000));
Run("14. Giá mua <= 0", () => new Microphone("MI90", "Micro", 2023, 0));
Run("15. Năm sử dụng ở tương lai", () => new Microphone("MI91", "Micro", 2999, 1000000));


// Các trạng thái của một thiết bị
public enum DeviceStatus
{
    Available,        // Sẵn sàng cho mượn
    Borrowed,         // Đang được mượn
    UnderMaintenance  // Đang bảo trì
}

// LỚP NGOẠI LỆ 
// Các lớp này giúp phân loại lỗi chi tiết thay vì dùng Exception chung chung.

// Lỗi gốc của hệ thống mượn trả
public class BorrowingException : Exception
{
    public BorrowingException(string message) : base(message) { }
    public BorrowingException(string message, Exception innerException) : base(message, innerException) { }
}

// Lỗi khi cố mượn thiết bị không ở trạng thái "Available"
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

// Lỗi khi mượn quá số lượng thiết bị cho phép
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

// Lỗi khi mượn quá số ngày cho phép của thiết bị
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

// Lỗi khi thiết bị bị trùng (thêm 2 lần vào 1 phòng/phiếu)
public class DuplicateDeviceException : BorrowingException
{
    public DuplicateDeviceException(string deviceId)
        : base($"Thiết bị {deviceId} đã tồn tại") { }
}

// Lỗi khi cố trả một thiết bị không có trong phiếu
public class DeviceNotInSlipException : BorrowingException
{
    public DeviceNotInSlipException(string deviceId)
        : base($"Thiết bị {deviceId} không thuộc phiếu mượn") { }
}

// Lỗi khi trả lại một thiết bị đã được trả trước đó
public class DeviceAlreadyReturnedException : BorrowingException
{
    public DeviceAlreadyReturnedException(string deviceId)
        : base($"Thiết bị {deviceId} đã được trả trước đó") { }
}

// Lỗi khi ngày trả không hợp lệ (ví dụ: ngày trả < ngày mượn)
public class InvalidReturnDateException : BorrowingException
{
    public InvalidReturnDateException(string message) : base(message) { }
}

// Devices/Device.cs 
// Lớp trừu tượng (gốc) cho mọi loại thiết bị
public abstract class Device
{
    public string DeviceId { get; }            
    public string Name { get; }
    public int YearInUse { get; }
    public decimal Price { get; }
    public DeviceStatus Status { get; protected set; }   

    protected Device(string deviceId, string name, int yearInUse, decimal price)
    {
        // Kiểm tra tính hợp lệ của dữ liệu đầu vào
        if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("Mã thiết bị không được rỗng");
        if (price <= 0) throw new ArgumentException("Giá mua phải lớn hơn 0");
        if (yearInUse > DateTime.Now.Year) throw new ArgumentException("Năm sử dụng không được lớn hơn năm hiện tại");

        DeviceId = deviceId;
        Name = name;
        YearInUse = yearInUse;
        Price = price;
        Status = DeviceStatus.Available; // Mặc định tạo ra là sẵn sàng
    }

    // Đổi trạng thái sang bảo trì
    public void SendToMaintenance()
    {
        if (Status == DeviceStatus.Borrowed)
            throw new InvalidOperationException("Thiết bị đang được mượn, chưa thể bảo trì");
        Status = DeviceStatus.UnderMaintenance;
    }

    // Hàm trừu tượng để lớp con tự tính tiền bảo trì
    public abstract decimal CalculateMaintenanceCost();

    public override string ToString()
    {
        return $"{DeviceId} | {Name} | {YearInUse} | {Price} | {Status}";
    }
}

// Devices/TeachingDevice.cs 
// Kế thừa Device, dành riêng cho thiết bị dạy học (giới hạn ngày mượn và phí phạt)
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

    // Đánh dấu là đã được mượn
    public void MarkAsBorrowed()
    {
        if (Status != DeviceStatus.Available)
            throw new DeviceUnavailableException(DeviceId, Status);
        Status = DeviceStatus.Borrowed;
    }

    // Đánh dấu là sẵn sàng (thường dùng khi trả thiết bị)
    public void MarkAsAvailable()
    {
        Status = DeviceStatus.Available;
    }
}

// LỚP THIẾT BỊ CỤ THỂ 

// Điều khiển điều hòa
public class AirConRemote : TeachingDevice
{
    private const int MaxDays = 3;
    private const decimal LateFee = 20000;
    private const decimal CostRate = 0.05m;

    public AirConRemote(string deviceId, string name, int yearInUse, decimal price)
        : base(deviceId, name, yearInUse, price, MaxDays, LateFee) { }

    public override decimal CalculateMaintenanceCost() => Price * CostRate;
}

// Micro
public class Microphone : TeachingDevice
{
    private const int MaxDays = 1;
    private const decimal LateFee = 10000;
    private const decimal CostRate = 0.10m;

    public Microphone(string deviceId, string name, int yearInUse, decimal price)
        : base(deviceId, name, yearInUse, price, MaxDays, LateFee) { }

    public override decimal CalculateMaintenanceCost() => Price * CostRate;
}

// Điều khiển máy chiếu
public class ProjectorRemote : TeachingDevice
{
    private const int MaxDays = 3;
    private const decimal LateFee = 30000;
    private const decimal CostRate = 0.08m;
    private const decimal FixedCost = 50000; // Có thêm phụ phí cố định

    public ProjectorRemote(string deviceId, string name, int yearInUse, decimal price)
        : base(deviceId, name, yearInUse, price, MaxDays, LateFee) { }

    public override decimal CalculateMaintenanceCost() => Price * CostRate + FixedCost;
}

// Borrowers/IBorrower.cs 
// Interface cho đối tượng người mượn
public interface IBorrower
{
    string BorrowerId { get; }
    string FullName { get; }
    int GetMaximumDevices(); // Lấy số lượng thiết bị tối đa được mượn
}

// Lớp gốc cho người mượn
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

// Giảng viên (được mượn tối đa 3)
public class Lecturer : Borrower
{
    private const int MaxDevices = 3;
    public Lecturer(string borrowerId, string fullName) : base(borrowerId, fullName) { }
    public override int GetMaximumDevices() => MaxDevices;
}

// Trợ giảng (được mượn tối đa 2)
public class TeachingAssistant : Borrower
{
    private const int MaxDevices = 2;
    public TeachingAssistant(string borrowerId, string fullName) : base(borrowerId, fullName) { }
    public override int GetMaximumDevices() => MaxDevices;
}

// Models/ClassRoom.cs 
// Lớp đại diện cho phòng học, chứa danh sách các thiết bị trong phòng
public class ClassRoom
{
    public string RoomId { get; }
    public string RoomName { get; }
    public int Capacity { get; }
    private readonly List<Device> _devices = new(); // Danh sách thiết bị hiện có

    public ClassRoom(string roomId, string roomName, int capacity)
    {
        RoomId = roomId;
        RoomName = roomName;
        Capacity = capacity;
    }

    public void AddDevice(Device device)
    {
        if (device == null) throw new ArgumentNullException(nameof(device));
        if (FindDevice(device.DeviceId) != null) throw new DuplicateDeviceException(device.DeviceId);
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

    // Tính tổng tiền bảo trì của toàn bộ thiết bị trong phòng
    public decimal CalculateAnnualMaintenanceCost()
    {
        return _devices.Sum(d => d.CalculateMaintenanceCost());
    }

    // Lọc ra các thiết bị cần đi bảo trì (đang UnderMaintenance HOẶC dùng quá 5 năm)
    public List<Device> GetDevicesRequiringMaintenance()
    {
        return _devices
            .Where(d => d.Status == DeviceStatus.UnderMaintenance || DateTime.Now.Year - d.YearInUse > 5)
            .ToList();
    }
}

//Models/BorrowItem.cs 
// Chi tiết của 1 mục trong phiếu mượn (đại diện cho 1 thiết bị được mượn)
public class BorrowItem
{
    public TeachingDevice Device { get; }
    public DateTime BorrowedDate { get; }
    public DateTime DueDate { get; } // Hạn trả
    public int FromPeriod { get; }
    public int ToPeriod { get; }
    public DateTime? ReturnedDate { get; private set; } // Ngày thực trả (null nếu chưa trả)
    public bool IsReturned => ReturnedDate != null;

    public BorrowItem(TeachingDevice device, DateTime borrowedDate, DateTime dueDate,
                      int fromPeriod = 1, int toPeriod = 1)
    {
        if (device == null) throw new ArgumentNullException(nameof(device));
        if (dueDate < borrowedDate) throw new ArgumentException("Hạn trả phải sau hoặc bằng ngày mượn");
        if (fromPeriod < 1 || toPeriod < fromPeriod) throw new ArgumentException("Tiết kết thúc phải lớn hơn hoặc bằng tiết bắt đầu (và tiết bắt đầu >= 1)");

        Device = device;
        BorrowedDate = borrowedDate;
        DueDate = dueDate;
        FromPeriod = fromPeriod;
        ToPeriod = toPeriod;
        ReturnedDate = null;
    }

    // Ghi nhận trả thiết bị
    public void Return(DateTime returnedDate)
    {
        if (IsReturned) throw new DeviceAlreadyReturnedException(Device.DeviceId);
        if (returnedDate < BorrowedDate) throw new InvalidReturnDateException("Ngày trả không được trước ngày mượn");
        ReturnedDate = returnedDate;
    }

    // Tính tiền phạt (số ngày muộn * phí phạt/ngày của thiết bị đó)
    public decimal CalculateLateFee()
    {
        if (ReturnedDate == null) return 0;
        int lateDays = (ReturnedDate.Value.Date - DueDate.Date).Days;
        return lateDays > 0 ? lateDays * Device.LateFeePerDay : 0;
    }
}

// Models/BorrowingSlip.cs
// Phiếu mượn thiết bị
public class BorrowingSlip
{
    public string SlipId { get; }
    public IBorrower Borrower { get; } // Người mượn
    public string TeachingRoom { get; }
    public DateTime BorrowedDate { get; }

    private readonly List<BorrowItem> _items = new(); // Danh sách các thiết bị trong phiếu
    public IReadOnlyList<BorrowItem> Items => _items.AsReadOnly();

    public BorrowingSlip(string slipId, IBorrower borrower, string teachingRoom, DateTime borrowedDate)
    {
        if (string.IsNullOrWhiteSpace(slipId)) throw new ArgumentException("Mã phiếu không hợp lệ");
        if (borrower == null) throw new ArgumentNullException(nameof(borrower));
        if (string.IsNullOrWhiteSpace(teachingRoom)) throw new ArgumentException("Phòng dạy không hợp lệ");

        SlipId = slipId;
        Borrower = borrower;
        TeachingRoom = teachingRoom;
        BorrowedDate = borrowedDate;
    }

    private BorrowItem? FindItem(string deviceId)
    {
        return _items.FirstOrDefault(i => i.Device.DeviceId == deviceId);
    }

    // Hàm kiểm tra các điều kiện nghiệp vụ trước khi cho phép mượn
    private void ValidateBorrowing(TeachingDevice device, int borrowDays)
    {
        if (device == null) throw new ArgumentNullException(nameof(device));

        // Vượt quá số ngày mượn cho phép của loại thiết bị đó
        if (borrowDays <= 0 || borrowDays > device.MaxBorrowDays)
            throw new InvalidBorrowPeriodException(device.DeviceId, borrowDays, device.MaxBorrowDays);

        // Thiết bị đã có trong phiếu mượn này rồi
        if (FindItem(device.DeviceId) != null)
            throw new DuplicateDeviceException(device.DeviceId);

        // Người mượn vượt quá số lượng tối đa của họ (Giảng viên: 3, Trợ giảng: 2)
        if (_items.Count >= Borrower.GetMaximumDevices())
            throw new BorrowLimitExceededException(Borrower.BorrowerId, Borrower.GetMaximumDevices());

        // Thiết bị không ở trạng thái sẵn sàng
        if (device.Status != DeviceStatus.Available)
            throw new DeviceUnavailableException(device.DeviceId, device.Status);
    }

    // Thêm thiết bị vào phiếu mượn
    public void AddDevice(TeachingDevice device, int borrowDays, int fromPeriod = 1, int toPeriod = 1)
    {
        ValidateBorrowing(device, borrowDays);

        var item = new BorrowItem(device, BorrowedDate, BorrowedDate.AddDays(borrowDays), fromPeriod, toPeriod);
        device.MarkAsBorrowed(); // Chuyển trạng thái thiết bị thành Đang mượn
        _items.Add(item);
    }

    // Trả một thiết bị trong phiếu, trả về tiền phạt muộn (nếu có)
    public decimal ReturnDevice(string deviceId, DateTime returnedDate)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("Mã thiết bị không hợp lệ");

        BorrowItem item = FindItem(deviceId) ?? throw new DeviceNotInSlipException(deviceId);

        item.Return(returnedDate);
        item.Device.MarkAsAvailable(); // Trả lại trạng thái Sẵn sàng cho thiết bị
        return item.CalculateLateFee();
    }

    // Tính tổng tiền phạt của tất cả thiết bị trong phiếu
    public decimal CalculateTotalLateFee()
    {
        return _items.Sum(i => i.CalculateLateFee());
    }
}
