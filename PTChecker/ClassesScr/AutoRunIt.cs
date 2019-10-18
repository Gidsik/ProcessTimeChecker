using System;
using Microsoft.Win32; //need to work with registry
using System.Reflection; //use it if you want to get path of your app
using System.Windows.Forms;

public static class AutoRunIt
{
	//set autorun for .exe file at [path] or delete that file from autorun if [autorun]==[false]
	public static bool SetAutoRun(bool autorun)
	{
		//edit name for your application
		string name = Application.ProductName;
        string exePath = Assembly.GetExecutingAssembly().Location;
		
		RegistryKey reg;
		reg = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\");
		
		try
		{
			if (autorun)
			{
				reg.SetValue(name, exePath);
			}
			else
			{
				reg.DeleteValue(name);
			}
			//reg.Flush();
			reg.Close();
		}
		catch
		{ 
			return false;
		}
		
		return true;
	}
	
}
