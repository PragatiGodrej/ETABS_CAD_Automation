using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using ETABSv1;

namespace ETABS_CAD_Automation.Core
{
    /// <summary>
    /// Handles connection and initialization of ETABS
    /// </summary>
    public class ETABSController
    {
        #region Windows API Imports
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const int VK_RETURN = 0x0D;
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        #endregion

        public cOAPI EtabsObject { get; private set; }
        public cSapModel SapModel { get; private set; }

        /// <summary>
        /// Connect to ETABS or start a new instance
        /// </summary>
        public bool Connect()
        {
            try
            {
                // Try to attach to running ETABS instance
                if (!AttachToRunningETABS())
                {
                    // Start new ETABS instance
                    if (!StartNewETABS())
                    {
                        return false;
                    }
                }

                // Initialize model
                InitializeModel();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"ETABS Connection Error:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Try to attach to already running ETABS
        /// </summary>
        private bool AttachToRunningETABS()
        {
            try
            {
                EtabsObject = (cOAPI)Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Start new ETABS instance
        /// </summary>
        private bool StartNewETABS()
        {
            try
            {
                cHelper helper = new Helper();
                EtabsObject = helper.CreateObjectProgID("CSI.ETABS.API.ETABSObject");
                EtabsObject.ApplicationStart();

                // Wait for ETABS to start
                Thread.Sleep(5000);

                // Handle login dialog if it appears
                HandleETABSLogin();

                // Additional wait for full initialization
                Thread.Sleep(8000);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to start ETABS:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Initialize new blank model with proper units
        /// </summary>
        private void InitializeModel()
        {
            SapModel = EtabsObject.SapModel;
            SapModel.InitializeNewModel();

            // Set units BEFORE creating model
            SapModel.SetPresentUnits(eUnits.kN_m_C);

            // Create new blank model
            SapModel.File.NewBlank();

            // Set units AFTER creating model (ensures they stick)
            SapModel.SetPresentUnits(eUnits.kN_m_C);

            // Refresh view
            SapModel.View.RefreshView(0, false);
        }

        /// <summary>
        /// Handle ETABS login dialog by simulating Enter key press
        /// </summary>
        private void HandleETABSLogin()
        {
            IntPtr hWnd = IntPtr.Zero;
            int attempts = 0;
            const int maxAttempts = 20;

            // Try to find ETABS window
            while (hWnd == IntPtr.Zero && attempts < maxAttempts)
            {
                hWnd = FindWindow(null, "ETABS");
                Thread.Sleep(500);
                attempts++;
            }

            // If found, bring to foreground and press Enter
            if (hWnd != IntPtr.Zero)
            {
                SetForegroundWindow(hWnd);
                Thread.Sleep(1000);

                // Simulate Enter key press
                keybd_event(VK_RETURN, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }

        /// <summary>
        /// Disconnect from ETABS
        /// </summary>
        public void Disconnect()
        {
            if (SapModel != null)
            {
                SapModel = null;
            }

            if (EtabsObject != null)
            {
                EtabsObject = null;
            }
        }
    }
}