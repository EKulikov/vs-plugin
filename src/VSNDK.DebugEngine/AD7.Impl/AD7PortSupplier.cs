﻿//* Copyright 2010-2011 Research In Motion Limited.
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//* http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.VisualStudio.Shell;

namespace VSNDK.DebugEngine
{
    /// <summary>
    /// This interface supplies ports to the session debug manager (SDM). (http://msdn.microsoft.com/en-ca/library/bb145819.aspx)
    /// It is not implemented yet, but it seems to be needed to enable updating the list of processes in the "attach to process" 
    /// user interface.
    /// </summary>
    [ComVisible(true)]
    [Guid("BDC2218C-D50C-4A5A-A2F6-66BDC94FF8D6")]
    public class AD7PortSupplier : IDebugPortSupplier3, IDebugPortSupplierDescription2
    {
        private string m_name;
        private string m_description;
        private string m_toolsPath = "";
        Dictionary<Guid, AD7Port> m_ports = new Dictionary<Guid, AD7Port>();

        public AD7PortSupplier()
        {
            m_name = "BlackBerry";
            m_description = "The BlackBerry transport lets you select a process that is running in a BlackBerry Device/Simulator";

            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkPluginRegKey = null;
            string DeviceIP = "";
            string DevicePassword = "";
            string SimulatorIP = "";
            string SimulatorPassword = "";

            try
            {
                rkPluginRegKey = rkHKCU.OpenSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                m_toolsPath = rkPluginRegKey.GetValue("NDKHostPath").ToString() + "/usr/bin";
                DeviceIP = rkPluginRegKey.GetValue("device_IP").ToString();
                if ((DeviceIP != "") && (DeviceIP != null))
                {
                    DevicePassword = rkPluginRegKey.GetValue("device_password").ToString();
                    if ((DevicePassword != "") && (DevicePassword != null))
                    {
                        try
                        {
                            DevicePassword = Decrypt(DevicePassword);
                        }
                        catch
                        {
                            DevicePassword = "";
                        }
                    }
                    if (DevicePassword == "")
                    {
                        MessageBox.Show("Missing Device password", "Missing Device Password", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    DeviceIP = "";
                }
                SimulatorIP = rkPluginRegKey.GetValue("simulator_IP").ToString();
                SimulatorPassword = rkPluginRegKey.GetValue("simulator_password").ToString();
                if ((SimulatorPassword != "") && (SimulatorPassword != null))
                {
                    try
                    {
                        SimulatorPassword = Decrypt(SimulatorPassword);
                    }
                    catch
                    {
                        SimulatorPassword = "";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "Microsoft Visual Studio", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }

            rkPluginRegKey.Close();
            rkHKCU.Close();

            if ((DeviceIP != "") && (DevicePassword != ""))
            {
                IDebugPort2 p;
                AddPort(new AD7PortRequest("Device: " + DeviceIP + "-" + DevicePassword), out p);
            }

            if (SimulatorIP != "")
            {
                IDebugPort2 p;
                AddPort(new AD7PortRequest("Simulator: " + SimulatorIP + "-" + SimulatorPassword), out p);
            }
        }

        AD7Port CreatePort(AD7PortRequest rq)
        {
            string portname;
            Guid guid;
            bool isSimulator = false;
            rq.GetPortName(out portname);
            string password = portname.Substring(portname.IndexOf('-') + 1);
            portname = portname.Remove(portname.IndexOf('-'));
            if (portname.Substring(0, 6) == "Device")
                guid = new Guid("{69519DBB-5329-4CCE-88A9-EC1628AD99C2}");
            else
            {
                guid = new Guid("{25040BDD-6683-4D5C-8EFA-EB4DDF5CA08E}");
                isSimulator = true;
            }

            return new AD7Port(this, rq, guid, portname, password, isSimulator, m_toolsPath);
        }

        #region Implementation of IDebugPortSupplier2
        
        /// <summary>
        /// Adds a port. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb161980.aspx)
        /// </summary>
        /// <param name="pRequest"> An IDebugPortRequest2 object that describes the port to be added. </param>
        /// <param name="ppPort"> Returns an IDebugPort2 object that represents the port. </param>
        /// <returns> Not implemented. It should returns S_OK if successful; or an error code. </returns>
        public int AddPort(IDebugPortRequest2 pRequest, out IDebugPort2 ppPort)
        {
            AD7PortRequest pr = (AD7PortRequest)pRequest;
            var port = CreatePort(pr);
            m_ports.Add(port.Guid, port);
            ppPort = port;
            return VSConstants.S_OK; 
        }


        /// <summary>
        /// Verifies that a port supplier can add new ports. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb145880.aspx)
        /// </summary>
        /// <returns> Not implemented. It should returns S_OK if the port can be added, or S_FALSE to indicate no ports can be added 
        /// to this port supplier. </returns>
        public int CanAddPort()
        {
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Retrieves a list of all the ports supplied by a port supplier. Not implemented. 
        /// (http://msdn.microsoft.com/en-ca/library/bb146984.aspx)
        /// </summary>
        /// <param name="ppEnum"> Returns an IEnumDebugPorts2 object containing a list of ports supplied. </param>
        /// <returns> Not implemented. It should returns S_OK if successful; or an error code. </returns>
        public int EnumPorts(out IEnumDebugPorts2 ppEnum)
        {
            AD7Port[] ports = new AD7Port[m_ports.Count()];
            int i = 0;
            foreach (var p in m_ports)
            {
                ports[i] = p.Value;
                i++;
            }
            ppEnum = new AD7PortEnum(ports);
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets a port from a port supplier. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb161812.aspx)
        /// </summary>
        /// <param name="guidPort"> Globally unique identifier (GUID) of the port. </param>
        /// <param name="ppPort"> Returns an IDebugPort2 object that represents the port. </param>
        /// <returns> Not implemented. It should returns S_OK if successful; or an error code. </returns>
        public int GetPort(ref Guid guidPort, out IDebugPort2 ppPort)
        {
            ppPort = m_ports[guidPort];
            return VSConstants.S_OK; 
        }


        /// <summary>
        /// Gets the port supplier identifier. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb146617.aspx)
        /// </summary>
        /// <param name="pguidPortSupplier"> Returns the GUID of the port supplier. </param>
        /// <returns> Not implemented. It should returns S_OK if successful; or an error code. </returns>
        public int GetPortSupplierId(out Guid pguidPortSupplier)
        {
            pguidPortSupplier = this.GetType().GUID;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the port supplier name. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb162136.aspx)
        /// </summary>
        /// <param name="pbstrName"> Returns the name of the port supplier. </param>
        /// <returns> Not implemented. It should returns S_OK if successful; or an error code. </returns>
        public int GetPortSupplierName(out string pbstrName)
        {
            pbstrName = m_name;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Removes a port. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb162306.aspx)
        /// </summary>
        /// <param name="pPort"> An IDebugPort2 object that represents the port to be removed. </param>
        /// <returns> Not implemented. It should returns S_OK if successful; or an error code. </returns>
        public int RemovePort(IDebugPort2 pPort)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Implementation of IDebugPortSupplier3

        public int CanPersistPorts()
        {
            return VSConstants.S_OK;
        }

        public int EnumPersistedPorts(BSTR_ARRAY PortNames, out IEnumDebugPorts2 ppEnum)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Implementation of IDebugPortSupplierDescription2

        public int GetDescription(enum_PORT_SUPPLIER_DESCRIPTION_FLAGS[] pdwFlags, out string pbstrText)
        {
            pbstrText = m_description;
            return VSConstants.S_OK;
        }

        #endregion

        /// <summary>
        /// Decrypts a given string.
        /// </summary>
        /// <param name="cipher">A base64 encoded string that was created
        /// through the <see cref="Encrypt(string)"/> or
        /// <see cref="Encrypt(SecureString)"/> extension methods.</param>
        /// <returns>The decrypted string.</returns>
        /// <remarks>Keep in mind that the decrypted string remains in memory
        /// and makes your application vulnerable per se. If runtime protection
        /// is essential, <see cref="SecureString"/> should be used.</remarks>
        /// <exception cref="ArgumentNullException">If <paramref name="cipher"/>
        /// is a null reference.</exception>
        public string Decrypt(string cipher)
        {
            if (cipher == null) throw new ArgumentNullException("cipher");

            //parse base64 string
            byte[] data = Convert.FromBase64String(cipher);

            //decrypt data
            byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine);

            return Encoding.Unicode.GetString(decrypted);
        }
    }
}