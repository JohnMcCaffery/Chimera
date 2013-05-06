timeout 20

cd C:\Users\OpenVirtualWorlds\Documents\John\Chimera

git pull
git add .
git commit -m "Startup log push - %DATE% %TIME%"
git push

git pull
cd Bin
git add .
git commit -m "Startup log push - %DATE% %TIME%"

cd C:\Users\OpenVirtualWorlds\Desktop\Opensim-Timespan\
start "OpenSim" /MAX OpenSim.exe
cd C:\Users\OpenVirtualWorlds\Documents\John\Chimera\

timeout 60

launch.bat
