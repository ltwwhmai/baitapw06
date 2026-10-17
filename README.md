```mermaid
classDiagram
    direction TB

    class DeviceStatus {
        <<enumeration>>
        Available
        Borrowed
        UnderMaintenance
    }

    class Device {
        <<abstract>>
        +string DeviceId
        +string Name
        +int YearInUse
        +decimal Price
        +DeviceStatus Status
        +SendToMaintenance()
        +CalculateMaintenanceCost() decimal*
        +ToString() string
    }

    class TeachingDevice {
        <<abstract>>
        +int MaxBorrowDays
        +decimal LateFeePerDay
        +MarkAsBorrowed()
        +MarkAsAvailable()
    }

    class AirConRemote {
        +CalculateMaintenanceCost() decimal
    }
    class Microphone {
        +CalculateMaintenanceCost() decimal
    }
    class ProjectorRemote {
        +CalculateMaintenanceCost() decimal
    }

    class ClassRoom {
        +string RoomId
        +string RoomName
        +int Capacity
        +AddDevice(Device device)
        +RemoveDevice(string deviceId) bool
        +FindDevice(string deviceId) Device
        +CalculateAnnualMaintenanceCost() decimal
        +GetDevicesRequiringMaintenance() List~Device~
    }

    Device --> DeviceStatus : có trạng thái
    Device <|-- TeachingDevice
    TeachingDevice <|-- AirConRemote
    TeachingDevice <|-- Microphone
    TeachingDevice <|-- ProjectorRemote
    ClassRoom o-- Device : kết tập

classDiagram
    direction TB

    class IBorrower {
        <<interface>>
        +string BorrowerId
        +string FullName
        +GetMaximumDevices() int
    }

    class Borrower {
        <<abstract>>
        +GetMaximumDevices() int*
    }

    class Lecturer {
        +GetMaximumDevices() int
    }
    class TeachingAssistant {
        +GetMaximumDevices() int
    }

    class TeachingDevice {
        <<abstract>>
    }

    class BorrowItem {
        +TeachingDevice Device
        +DateTime BorrowedDate
        +DateTime DueDate
        +int FromPeriod
        +int ToPeriod
        +DateTime? ReturnedDate
        +bool IsReturned
        +Return(DateTime returnedDate)
        +CalculateLateFee() decimal
    }

    class BorrowingSlip {
        +string SlipId
        +IBorrower Borrower
        +string TeachingRoom
        +DateTime BorrowedDate
        +IReadOnlyList~BorrowItem~ Items
        +AddDevice(TeachingDevice device, int borrowDays, int fromPeriod, int toPeriod)
        +ReturnDevice(string deviceId, DateTime returnedDate) decimal
        +CalculateTotalLateFee() decimal
    }

    class BorrowingException
    class DeviceUnavailableException
    class BorrowLimitExceededException
    class InvalidBorrowPeriodException
    class DuplicateDeviceException
    class DeviceNotInSlipException
    class DeviceAlreadyReturnedException
    class InvalidReturnDateException

    IBorrower <|.. Borrower
    Borrower <|-- Lecturer
    Borrower <|-- TeachingAssistant

    BorrowingSlip --> IBorrower : người mượn
    BorrowingSlip *-- BorrowItem : hợp thành
    BorrowItem --> TeachingDevice : tham chiếu

    BorrowingException <|-- DeviceUnavailableException
    BorrowingException <|-- BorrowLimitExceededException
    BorrowingException <|-- InvalidBorrowPeriodException
    BorrowingException <|-- DuplicateDeviceException
    BorrowingException <|-- DeviceNotInSlipException
    BorrowingException <|-- DeviceAlreadyReturnedException
    BorrowingException <|-- InvalidReturnDateException
