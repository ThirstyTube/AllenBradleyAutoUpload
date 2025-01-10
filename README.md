# AllenBradleyAutoUpload

This was written as a solution to a manual task of backing up PLCs in a production space every month. 
****Notes before using****
You will need to install Rockwells SDK library you can get it from the download center 

You will need to have a Studio 5000 Full license in order to use this. If you dont it will error for no license found. 

You will need Rs Linx Classic and an ethernet driver populated with the devices youre going to use. Default this uses AB_ETH-1 but you can change it to what ever you like.,

This only works with PLCs Firmware Ver 33 and up. 

You will also need studio 5000 versions installed that match the firmware of your machines. I.e if you have PLCs with ver 33,34,and36 you need all three of those versions of Studio installed on your PC

****How to use****

The Main branch features a version that requires a little bit of editing to use it but is the easiest way to just pick it up and try it out.
I use a version that I will be posting as a branch that uses a SQL db to store all of the Machine information.

First Edit(optional) 

You should only need to change this if you install your SDK package in a non default spot. 
    ExePath = @"C:\\Program Files (x86)\\Rockwell Software\\Studio 5000\\Logix Designer SDK\\LdSdkServer.exe"

Second Edit

This section is how you hardcode the required information. 

    ("C:\\Backups\\Machine1", "Machine1", "192.168.0.21", "CLX"),
    ("C:\\Backups\\Machine2", "Machine2", "192.168.0.22", "CLX"),
    ("C:\\Backups\\Machine3", "Machine3", "192.168.0.23", "COM"),
    
  The First part is where you are sending the uploaded file to. I personally always name the main folder after the PLC file inside. 

  The second part is the name of the file. When uploading a file the solution will concat Name with todays date so the example would be Machine1_YYYY_MM_DD.ACD

  The third part is the IP Address of the PLC youre looking for. 

  CLX and COM is to designate if the PLC youre uploading is a ControlLogix(CLX) or a CompactLogix(COM) This effects the solution later when communicating with the PLC due to network path diffrences between the two.  


  Third Edit.(optional) 

    string ip = device.Files == "CLX"
      ? $"AB_ETH-1\\{device.IP}\\Backplane\\0"
      : $"AB_ETH-1\\{device.IP}"
    
This is where the CLX entry comes in as. ControlLogix PLCs network path includes the backplane and slot number. 
All CLX processors in my space are in slot zero. you could modify the code to accomodate this but I didnt because all machines in my enviorment have processors in slot 0 
This is also where you may need to edit the network path to what your driver is named in RS Linx Classic. I use default first driver AB_ETH-1. This can be changed to match your needs. 

ALL PLCS YOU WISH TO BACK UP MUST BE CONFIGURED IN THIS DRIVER OR THE BACK UP WILL FAIL. 


****Notes about function****
This solution will check to see if the PLC can be reached by host machine or not. if a PLC fails a ping test it will skip trying to back it up. 

This solution will also make a folder in your main dir called Archive. 
When it backs up a PLC it also will scan the main dir and move currently present .ACD files into the folder Archive. 
It will then leave the newest upload only remaining in main dir. 
It will fail to move a file if it is opened elsewere or otherwise getting edited from other sources. 

This Can be run on a schduled basis with Task Scheduler. 

