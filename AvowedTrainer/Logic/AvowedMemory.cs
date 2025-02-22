using MemUtil;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AvowedTrainer.Logic
{
    internal class AvowedMemory
    {
        public MemoryWatcherList? Watchers { get; private set; }
        public bool IsInitialized { get; private set; } = false;

        private Process? proc;

        public bool UpdateState()
        {
            if (!IsHooked() || !IsInitialized)
            {
                IsInitialized = false;
                Hook();
                Thread.Sleep(1000);
                return false;
            }

            try
            {
                Watchers.UpdateAll(proc);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        private bool IsHooked()
        {
            return proc != null && !proc.HasExited;
        }

        private void Hook()
        {
            List<Process> processList = Process.GetProcesses().ToList().FindAll(x => Regex.IsMatch(x.ProcessName, "Avowed.*-Shipping"));
            if (processList.Count == 0)
            {
                proc = null;
                return;
            }
            proc = processList[0];

            if (IsHooked())
            {
                IsInitialized = Initialize();
            }
        }

        private bool Initialize()
        {

            nint localPlayerPtr;
            try
            {
                SignatureScanner scanner = new SignatureScanner(proc, proc.MainModule.BaseAddress, proc.MainModule.ModuleMemorySize);
                localPlayerPtr = GetLocalPlayerPtr();
                if (localPlayerPtr == IntPtr.Zero)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            const int OFFSET_CONTROLLER = 0x30;
            const int OFFSET_CHARACTER = 0x2E8;
            const int OFFSET_CAPSULE = 0x330;
            const int OFFSET_MOVEMENT = 0x328;
            Debug.WriteLine(localPlayerPtr.ToString("X8"));

            Watchers = [
                // LocalPlayer -> PlayerController -> PlayerCharacter -> PlayerCapsule -> Position
                new MemoryWatcher<double>(new DeepPointer(localPlayerPtr, OFFSET_CONTROLLER, OFFSET_CHARACTER, OFFSET_CAPSULE, 0x260)) { Name = "xPos" },
                new MemoryWatcher<double>(new DeepPointer(localPlayerPtr, OFFSET_CONTROLLER, OFFSET_CHARACTER, OFFSET_CAPSULE, 0x268)) { Name = "yPos" },
                new MemoryWatcher<double>(new DeepPointer(localPlayerPtr, OFFSET_CONTROLLER, OFFSET_CHARACTER, OFFSET_CAPSULE, 0x270)) { Name = "zPos" },

                // LocalPlayer -> PlayerController -> PlayerCharacter -> PlayerMovement -> Velocity
                new MemoryWatcher<double>(new DeepPointer(localPlayerPtr, OFFSET_CONTROLLER, OFFSET_CHARACTER, OFFSET_MOVEMENT, 0xB8)) { Name = "xVel" },
                new MemoryWatcher<double>(new DeepPointer(localPlayerPtr, OFFSET_CONTROLLER, OFFSET_CHARACTER, OFFSET_MOVEMENT, 0xC0)) { Name = "yVel" },
                new MemoryWatcher<double>(new DeepPointer(localPlayerPtr, OFFSET_CONTROLLER, OFFSET_CHARACTER, OFFSET_MOVEMENT, 0xC8)) { Name = "zVel" },

                // LocalPlayer -> PlayerController -> PlayerCharacter -> PlayerMovement -> MovementMode
                new MemoryWatcher<byte>(new DeepPointer(localPlayerPtr, OFFSET_CONTROLLER, OFFSET_CHARACTER, OFFSET_MOVEMENT, 0x201)) { Name = "movementMode" },

                // LocalPlayer -> PlayerController -> PlayerCharacter -> PlayerMovement -> MaxFlySpeed
                new MemoryWatcher<float>(new DeepPointer(localPlayerPtr, OFFSET_CONTROLLER, OFFSET_CHARACTER, OFFSET_MOVEMENT, 0x254)) { Name = "flySpeed" },

                // LocalPlayer -> PlayerController -> PlayerCharacter -> PlayerMovement -> MaxAcceleration
                new MemoryWatcher<float>(new DeepPointer(localPlayerPtr, OFFSET_CONTROLLER, OFFSET_CHARACTER, OFFSET_MOVEMENT, 0x25C)) { Name = "acceleration" },

                // LocalPlayer -> PlayerController -> PlayerCharacter -> PlayerMovement -> CheatFlying
                new MemoryWatcher<byte>(new DeepPointer(localPlayerPtr, OFFSET_CONTROLLER, OFFSET_CHARACTER, OFFSET_MOVEMENT, 0x50D)) { Name = "cheatFlying" },

                // LocalPlayer -> PlayerController -> PlayerCharacter -> EnableCollision
                new MemoryWatcher<byte>(new DeepPointer(localPlayerPtr, OFFSET_CONTROLLER, OFFSET_CHARACTER, 0x5D)) { Name = "collisionEnabled" },

                // LocalPlayer -> PlayerController -> PlayerCharacter -> CanBeDamaged
                new MemoryWatcher<byte>(new DeepPointer(localPlayerPtr, OFFSET_CONTROLLER, OFFSET_CHARACTER, 0x5A)) { Name = "canBeDamaged" },

                // LocalPlayer -> PlayerController -> ControlRotation
                new MemoryWatcher<double>(new DeepPointer(localPlayerPtr, OFFSET_CONTROLLER, 0x310)) { Name = "vLook" },
                new MemoryWatcher<double>(new DeepPointer(localPlayerPtr, OFFSET_CONTROLLER, 0x318)) { Name = "hLook" },
            ];


            try
            {
                Watchers.UpdateAll(proc);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        public void Write(string name, byte[] bytes)
        {
            if (!IsHooked() || !IsInitialized || !Watchers[name].DeepPtr.DerefOffsets(proc, out nint addr))
            {
                return;
            }

            try
            {
                _ = proc.WriteBytes(addr, bytes);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Write(string name, float fValue)
        {
            Write(name, BitConverter.GetBytes(fValue));
        }

        public void Write(string name, double dValue)
        {
            Write(name, BitConverter.GetBytes(dValue));
        }

        public void Write(string name, bool boolValue)
        {
            Write(name, BitConverter.GetBytes(boolValue));
        }

        public void Write(string name, byte bValue)
        {
            Write(name, new byte[] { bValue });
        }

        private IntPtr GetLocalPlayerPtr()
        {
            var scn = new SignatureScanner(proc, proc.MainModule.BaseAddress, proc.MainModule.ModuleMemorySize);
            var localPlayerTrg = new SigScanTarget(3, "48 89 35 ?? ?? ?? ?? 0F 10 0D") { OnFound = (p, s, ptr) => ptr + 0x4 + proc.ReadValue<int>(ptr) };
            return scn.Scan(localPlayerTrg);
        }

    }
}