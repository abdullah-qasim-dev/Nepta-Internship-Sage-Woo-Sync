using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.Client
{
    public class SageConnectionManager
    {
        private readonly string _companyPath;
        private readonly string _userName;
        private readonly string _password;
        private readonly string _workSpaceName;

        public SageConnectionManager(string companyPath, string userName, string password, string workSpaceName)
        {
            _companyPath = companyPath;
            _userName = userName;
            _password = password;
            _workSpaceName = workSpaceName;
        }

        public SageDataObject310.WorkSpace ConnectToSage()
        {
            SageDataObject310.SDOEngine oSDO = new SageDataObject310.SDOEngine();
            SageDataObject310.WorkSpace oWS = null;
            //string szDataPath1 = oSDO.SelectCompany(_companyPath);
            string szDataPath = _companyPath;

            if (szDataPath != string.Empty)
            {
                try
                {
                    oWS = (SageDataObject310.WorkSpace)oSDO.Workspaces.Add(_workSpaceName);
                    oWS.Connect(szDataPath, _userName, _password, "");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error connecting to Sage: {ex.Message}", ex);
                }
            }
            else
            {
                throw new Exception("Company selection was cancelled or failed.");
            }

            return oWS;
        }

        public void Disconnect(SageDataObject310.WorkSpace workspace)
        {
            if (workspace != null)
            {
                workspace.Disconnect();
                Marshal.ReleaseComObject(workspace);  // Release the COM object explicitly
                workspace = null; // Null the reference to help garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();  //
            }
        }
    }

}
