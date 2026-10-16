using System;
using System.Collections.Generic;

// ===================== ENUM =====================
public enum DeviceStatus
{
    Available,
    Borrowed,
    UnderMaintenance
}

// ===================== NGOAI LE =====================
public class BorrowingException : Exception
{
    public BorrowingException(string message) : base(message) { }
    public BorrowingException(string message, Exception innerException) : base(message, innerException) { }
}

public class DeviceUnavailableException : BorrowingException
{
    public string DeviceId { get; }
    public DeviceStatus CurrentStatus { get; }
    public DeviceUnavailableException(string ma_tb, DeviceStatus trang_thai)
        : base("Thiết bị " + ma_tb + " không sẵn sàng, trạng thái: " + trang_thai)
    {
        DeviceId = ma_tb;
        CurrentStatus = trang_thai;
    }
}

public class BorrowLimitExceededException : BorrowingException
{
    public string BorrowerId { get; }
    public int MaximumDevices { get; }
    public BorrowLimitExceededException(string ma_nguoi_muon, int toi_da)
        : base("Người mượn " + ma_nguoi_muon + " đã đạt tối đa " + toi_da + " thiết bị")
    {
        BorrowerId = ma_nguoi_muon;
        MaximumDevices = toi_da;
    }
}

public class InvalidBorrowPeriodException : BorrowingException
{
    public string DeviceId { get; }
    public int RequestedDays { get; }
    public int MaximumDays { get; }
    public InvalidBorrowPeriodException(string ma_tb, int so_ngay_muon, int so_ngay_toi_da)
        : base("Số ngày mượn " + so_ngay_muon + " không hợp lệ cho thiết bị " + ma_tb + " (tối đa " + so_ngay_toi_da + ")")
    {
        DeviceId = ma_tb;
        RequestedDays = so_ngay_muon;
        MaximumDays = so_ngay_toi_da;
    }
}

public class DuplicateDeviceException : BorrowingException
{
    public DuplicateDeviceException(string ma_tb)
        : base("Thiết bị " + ma_tb + " đã tồn tại") { }
}

public class DeviceNotInSlipException : BorrowingException
{
    public DeviceNotInSlipException(string ma_tb)
        : base("Thiết bị " + ma_tb + " không thuộc phiếu mượn") { }
}

public class DeviceAlreadyReturnedException : BorrowingException
{
    public DeviceAlreadyReturnedException(string ma_tb)
        : base("Thiết bị " + ma_tb + " đã được trả trước đó") { }
}

// Ngoai le them: ngay tra khong hop le (de co lop rieng cho tinh huong nay)
public class InvalidReturnDateException : BorrowingException
{
    public InvalidReturnDateException(string message) : base(message) { }
}

// ===================== THIET BI =====================
public abstract class Device
{
    public string DeviceId { get; }          // chi gan luc khoi tao
    public string Name { get; }
    public int YearInUse { get; }
    public decimal Price { get; }
    public DeviceStatus Status { get; set; }

    protected Device(string ma_tb, string ten, int nam_su_dung, decimal gia)
    {
        if (string.IsNullOrWhiteSpace(ma_tb))
            throw new ArgumentException("Mã thiết bị không được rỗng");
        if (gia <= 0)
            throw new ArgumentException("Giá mua phải lớn hơn 0");
        if (nam_su_dung > DateTime.Now.Year)
            throw new ArgumentException("Năm sử dụng không được lớn hơn năm hiện tại");

        DeviceId = ma_tb;
        Name = ten;
        YearInUse = nam_su_dung;
        Price = gia;
        Status = DeviceStatus.Available;
    }

    public abstract decimal CalculateMaintenanceCost();

    public override string ToString()
    {
        return DeviceId + " | " + Name + " | " + YearInUse + " | " + Price + " | " + Status;
    }
}

public abstract class TeachingDevice : Device
{
    public int MaxBorrowDays { get; }
    public decimal LateFeePerDay { get; }

    protected TeachingDevice(string ma_tb, string ten, int nam_su_dung, decimal gia,
                             int so_ngay_toi_da, decimal phi_tre_moi_ngay)
        : base(ma_tb, ten, nam_su_dung, gia)
    {
        MaxBorrowDays = so_ngay_toi_da;
        LateFeePerDay = phi_tre_moi_ngay;
    }

    public void MarkAsBorrowed() { Status = DeviceStatus.Borrowed; }
    public void MarkAsAvailable() { Status = DeviceStatus.Available; }
}

public class AirConRemote : TeachingDevice
{
    public AirConRemote(string ma_tb, string ten, int nam, decimal gia)
        : base(ma_tb, ten, nam, gia, 3, 20000) { }

    public override decimal CalculateMaintenanceCost() { return Price * 0.05m; }
}

public class Microphone : TeachingDevice
{
    public Microphone(string ma_tb, string ten, int nam, decimal gia)
        : base(ma_tb, ten, nam, gia, 1, 10000) { }

    public override decimal CalculateMaintenanceCost() { return Price * 0.10m; }
}

public class ProjectorRemote : TeachingDevice
{
    public ProjectorRemote(string ma_tb, string ten, int nam, decimal gia)
        : base(ma_tb, ten, nam, gia, 3, 30000) { }

    public override decimal CalculateMaintenanceCost() { return Price * 0.08m + 50000; }
}

// ===================== PHONG HOC =====================
public class ClassRoom
{
    public string RoomId { get; }
    public string RoomName { get; }
    public int Capacity { get; }
    private List<Device> ds_thiet_bi = new List<Device>();

    public ClassRoom(string ma_phong, string ten_phong, int suc_chua)
    {
        RoomId = ma_phong;
        RoomName = ten_phong;
        Capacity = suc_chua;
    }

    public void AddDevice(Device device)
    {
        if (device == null)
            throw new ArgumentNullException("device");
        if (FindDevice(device.DeviceId) != null)
            throw new DuplicateDeviceException(device.DeviceId);
        ds_thiet_bi.Add(device);
    }

    public bool RemoveDevice(string deviceId)
    {
        Device? tb = FindDevice(deviceId);
        if (tb == null) return false;
        ds_thiet_bi.Remove(tb);
        return true;
    }

    public Device? FindDevice(string deviceId)
    {
        foreach (Device tb in ds_thiet_bi)
        {
            if (tb.DeviceId == deviceId) return tb;
        }
        return null;
    }

    public decimal CalculateAnnualMaintenanceCost()
    {
        decimal tong = 0;
        foreach (Device tb in ds_thiet_bi)
        {
            tong += tb.CalculateMaintenanceCost();   // da hinh
        }
        return tong;
    }

    public List<Device> GetDevicesRequiringMaintenance()
    {
        List<Device> ket_qua = new List<Device>();
        foreach (Device tb in ds_thiet_bi)
        {
            bool dang_bao_tri = tb.Status == DeviceStatus.UnderMaintenance;
            bool dung_lau = DateTime.Now.Year - tb.YearInUse > 5;
            if (dang_bao_tri || dung_lau) ket_qua.Add(tb);
        }
        return ket_qua;
    }
}

// ===================== NGUOI MUON =====================
public interface IBorrower
{
    string BorrowerId { get; }
    string FullName { get; }
    int GetMaximumDevices();
}

public class Lecturer : IBorrower
{
    public string BorrowerId { get; }
    public string FullName { get; }
    public Lecturer(string ma, string ho_ten) { BorrowerId = ma; FullName = ho_ten; }
    public int GetMaximumDevices() { return 3; }
}

public class TeachingAssistant : IBorrower
{
    public string BorrowerId { get; }
    public string FullName { get; }
    public TeachingAssistant(string ma, string ho_ten) { BorrowerId = ma; FullName = ho_ten; }
    public int GetMaximumDevices() { return 2; }
}

// ===================== BORROW ITEM =====================
public class BorrowItem
{
    public TeachingDevice Device { get; }
    public DateTime BorrowedDate { get; }
    public DateTime DueDate { get; }
    public int FromPeriod { get; set; }
    public int ToPeriod { get; set; }
    public DateTime? ReturnedDate { get; private set; }
    public bool IsReturned { get { return ReturnedDate != null; } }

    public BorrowItem(TeachingDevice device, DateTime ngay_muon, DateTime han_tra)
    {
        if (device == null)
            throw new ArgumentNullException("device");
        if (han_tra < ngay_muon)
            throw new ArgumentException("Hạn trả phải sau hoặc bằng ngày mượn");

        Device = device;
        BorrowedDate = ngay_muon;
        DueDate = han_tra;
        FromPeriod = 1;
        ToPeriod = 1;
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
        int so_ngay_tre = (ReturnedDate.Value - DueDate).Days;
        if (so_ngay_tre <= 0) return 0;
        return so_ngay_tre * Device.LateFeePerDay;
    }
}

// ===================== PHIEU MUON =====================
public class BorrowingSlip
{
    public string SlipId { get; }
    public IBorrower Borrower { get; }
    public string TeachingRoom { get; }
    public DateTime BorrowedDate { get; }
    private List<BorrowItem> ds_muc = new List<BorrowItem>();
    public IReadOnlyList<BorrowItem> Items { get { return ds_muc; } }

    public BorrowingSlip(string ma_phieu, IBorrower nguoi_muon, string phong_day, DateTime ngay_muon)
    {
        if (string.IsNullOrWhiteSpace(ma_phieu))
            throw new ArgumentException("Mã phiếu không hợp lệ");
        if (nguoi_muon == null)
            throw new ArgumentNullException("nguoi_muon");
        if (string.IsNullOrWhiteSpace(phong_day))
            throw new ArgumentException("Phòng dạy không hợp lệ");

        SlipId = ma_phieu;
        Borrower = nguoi_muon;
        TeachingRoom = phong_day;
        BorrowedDate = ngay_muon;
    }

    // Kiem tra het, KHONG sua gi ca
    private void ValidateBorrowing(TeachingDevice device, int borrowDays)
    {
        if (device == null)
            throw new ArgumentNullException("device");
        if (borrowDays <= 0 || borrowDays > device.MaxBorrowDays)
            throw new InvalidBorrowPeriodException(device.DeviceId, borrowDays, device.MaxBorrowDays);
        if (TimMuc(device.DeviceId) != null)
            throw new DuplicateDeviceException(device.DeviceId);
        if (ds_muc.Count >= Borrower.GetMaximumDevices())
            throw new BorrowLimitExceededException(Borrower.BorrowerId, Borrower.GetMaximumDevices());
        if (device.Status != DeviceStatus.Available)
            throw new DeviceUnavailableException(device.DeviceId, device.Status);
    }

    private BorrowItem? TimMuc(string ma_tb)
    {
        foreach (BorrowItem muc in ds_muc)
        {
            if (muc.Device.DeviceId == ma_tb) return muc;
        }
        return null;
    }

    public void AddDevice(TeachingDevice device, int borrowDays)
    {
        ValidateBorrowing(device, borrowDays);

        BorrowItem muc = new BorrowItem(device, BorrowedDate, BorrowedDate.AddDays(borrowDays));
        device.MarkAsBorrowed();
        ds_muc.Add(muc);
    }

    public decimal ReturnDevice(string deviceId, DateTime returnedDate)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Mã thiết bị không hợp lệ");

        BorrowItem? muc = TimMuc(deviceId);
        if (muc == null)
            throw new DeviceNotInSlipException(deviceId);
        if (muc.IsReturned)
            throw new DeviceAlreadyReturnedException(deviceId);

        muc.Return(returnedDate);            // neu ngay tra sai se nem loi, thiet bi chua bi doi
        muc.Device.MarkAsAvailable();
        return muc.CalculateLateFee();
    }

    public decimal CalculateTotalLateFee()
    {
        decimal tong = 0;
        foreach (BorrowItem muc in ds_muc)
        {
            tong += muc.CalculateLateFee();
        }
        return tong;
    }
}

// ===================== CHUONG TRINH CHINH =====================
public class Program
{
    // Chay 1 tinh huong, bat loi o day (lop ung dung)
    static void thu(string ten_tinh_huong, Action hanh_dong)
    {
        Console.WriteLine("--- " + ten_tinh_huong);
        try
        {
            hanh_dong();
            Console.WriteLine("Không có lỗi (không mong đợi!)");
        }
        catch (BorrowingException loi)
        {
            Console.WriteLine("Bắt được lỗi nghiệp vụ: " + loi.Message);
        }
        catch (ArgumentException loi)
        {
            Console.WriteLine("Bắt được lỗi tham số: " + loi.Message);
        }
        finally
        {
            Console.WriteLine();
        }
    }

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        DateTime ngay_muon = new DateTime(2026, 10, 19);

        // ---------- 11.1 Luong thanh cong ----------
        Console.WriteLine("===== LUỒNG THÀNH CÔNG =====");
        try
        {
            Lecturer giang_vien = new Lecturer("GV01", "Nguyen Van A");
            ProjectorRemote may_chieu = new ProjectorRemote("MC01", "Điều khiển máy chiếu", 2024, 500000);
            AirConRemote dieu_hoa = new AirConRemote("DH01", "Điều khiển điều hòa", 2023, 300000);
            Microphone micro = new Microphone("MI01", "Micro không dây", 2022, 1000000);

            ClassRoom phong = new ClassRoom("P101", "Phòng 101", 50);
            phong.AddDevice(may_chieu);
            phong.AddDevice(dieu_hoa);
            phong.AddDevice(micro);
            Console.WriteLine("Chi phí bảo trì năm: " + phong.CalculateAnnualMaintenanceCost());

            BorrowingSlip phieu = new BorrowingSlip("PM01", giang_vien, "P101", ngay_muon);
            phieu.AddDevice(may_chieu, 2);
            phieu.AddDevice(micro, 1);

            // Tra may chieu dung han (han = ngay_muon + 2)
            decimal phi_1 = phieu.ReturnDevice("MC01", ngay_muon.AddDays(2));
            // Tra micro muon 2 ngay (han = ngay_muon + 1)
            decimal phi_2 = phieu.ReturnDevice("MI01", ngay_muon.AddDays(3));
            Console.WriteLine("Phí muộn máy chiếu: " + phi_1);
            Console.WriteLine("Phí muộn micro: " + phi_2);
            Console.WriteLine("Tổng phí muộn: " + phieu.CalculateTotalLateFee());

            foreach (BorrowItem muc in phieu.Items)
            {
                if (!muc.IsReturned)
                    Console.WriteLine(muc.Device.Name + ": CHƯA TRẢ");
                else if (muc.ReturnedDate > muc.DueDate)
                    Console.WriteLine(muc.Device.Name + ": TRẢ MUỘN");
                else
                    Console.WriteLine(muc.Device.Name + ": trả đúng hạn");
            }
        }
        catch (BorrowingException loi)
        {
            Console.WriteLine("Lỗi: " + loi.Message);
        }
        finally
        {
            Console.WriteLine("Kết thúc luồng thành công");
        }

        // ---------- 11.2 Luong phat sinh ngoai le ----------
        Console.WriteLine();
        Console.WriteLine("===== LUỒNG NGOẠI LỆ =====");

        thu("1. Mượn điều khiển điều hòa đang UnderMaintenance", () =>
        {
            AirConRemote dh = new AirConRemote("DH02", "Điều hòa", 2023, 300000);
            dh.Status = DeviceStatus.UnderMaintenance;
            BorrowingSlip p = new BorrowingSlip("PM02", new Lecturer("GV01", "A"), "P101", ngay_muon);
            p.AddDevice(dh, 1);
        });

        thu("2. Trợ giảng mượn thiết bị thứ ba", () =>
        {
            BorrowingSlip p = new BorrowingSlip("PM03", new TeachingAssistant("TG01", "B"), "P101", ngay_muon);
            p.AddDevice(new Microphone("MI10", "Micro 1", 2023, 1000000), 1);
            p.AddDevice(new Microphone("MI11", "Micro 2", 2023, 1000000), 1);
            p.AddDevice(new Microphone("MI12", "Micro 3", 2023, 1000000), 1);
        });

        thu("3. Thêm cùng một micro hai lần", () =>
        {
            Microphone mi = new Microphone("MI20", "Micro", 2023, 1000000);
            BorrowingSlip p = new BorrowingSlip("PM04", new Lecturer("GV01", "A"), "P101", ngay_muon);
            p.AddDevice(mi, 1);
            p.AddDevice(mi, 1);
        });

        thu("4. Mượn micro 3 ngày (giới hạn 1 ngày)", () =>
        {
            Microphone mi = new Microphone("MI30", "Micro", 2023, 1000000);
            BorrowingSlip p = new BorrowingSlip("PM05", new Lecturer("GV01", "A"), "P101", ngay_muon);
            p.AddDevice(mi, 3);
        });

        thu("5. Trả điều khiển máy chiếu không thuộc phiếu", () =>
        {
            BorrowingSlip p = new BorrowingSlip("PM06", new Lecturer("GV01", "A"), "P101", ngay_muon);
            p.ReturnDevice("MC99", ngay_muon.AddDays(1));
        });

        thu("6. Trả cùng một thiết bị hai lần", () =>
        {
            Microphone mi = new Microphone("MI40", "Micro", 2023, 1000000);
            BorrowingSlip p = new BorrowingSlip("PM07", new Lecturer("GV01", "A"), "P101", ngay_muon);
            p.AddDevice(mi, 1);
            p.ReturnDevice("MI40", ngay_muon.AddDays(1));
            p.ReturnDevice("MI40", ngay_muon.AddDays(1));
        });

        thu("7. Trả thiết bị với ngày trả trước ngày mượn", () =>
        {
            Microphone mi = new Microphone("MI50", "Micro", 2023, 1000000);
            BorrowingSlip p = new BorrowingSlip("PM08", new Lecturer("GV01", "A"), "P101", ngay_muon);
            p.AddDevice(mi, 1);
            p.ReturnDevice("MI50", ngay_muon.AddDays(-1));
        });
    }
}