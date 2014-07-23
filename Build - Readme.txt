
The project is structured as a Visual Studio 2010 solution (ScrewTurnWiki.sln).

In order to compile the application you can either build the solution in Visual Studio, or 
follow the instructions included in the Build directory.

The CHM documentation file (Help directory) is built using Microsoft Sandcastle [1] and 
Sandcastle Help File Builder [2]. The documentation is not necessary to compile or run the application.

Note: if you are using a 64-bit Windows edition, Sandcastle as well as HTML Help Workshop 
(distributed with Visual Studio 2008) are installed by default in "C:\Program Files (x86)\".
The SHFB project assumes that the installation directory is "C:\Program Files\".
In order to fix this issue without re-installing the applications or modifying the project file,
you can create a symbolic link using the "mklink" command-line tool (only supported in
Windows Server 2003/2008 and Windows Vista and 7 (you may need to open an elevated command prompt):

mklink /D Sandcastle "C:\Program Files (x86)\Sandcastle"
mklink /D "HTML Help Workshop" "C:\Program Files (x86)\HTML Help Workshop"

[1] http://www.codeplex.com/Sandcastle
[2] http://www.codeplex.com/SHFB
