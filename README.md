# Yer-İstasyonu

### What is ground station software and why is it used?
Ground station software is a software application used to analyze wirelessly communicating UAV or Rocket systems in the space and aviation industry. 
The ground station software I designed myself is a GUI interface software that takes multiple sensor data, processes and displays it into graphics.

The purpose of this software is specifically designed to display and inform the user about the avionics system in a rocket.
Orientation data is taken from sensors such as Mpu6050 or Mpu9250 and visualized with the openGL library, so the user can see the current status of the rocket more easily.
It provides an opportunity to observe. By visualizing GPS data, we aim to track the location of the rocket without using any GPS application.

## Setup
Before running the project, the following software must be installed on your computer.

.NET Framework (Whichever version was used in your project, for example .NET Framework 4.7.2)
Visual Studio (2019 or newer recommended)
Windows Forms development components must be installed.

* first step clone the project
```bash
git clone https://github.com/yarumiks/Ground_Control_Station_Software-V2.0.git
```
* go to the directory of the cloned project
```bash
cd Ground_Control_Station_Software-V2.0
```
* If visual studio is installed, run the file with `.sln` extension from here.

* If you want to run the project directly, run the `setup.exe` file inside the project after `clone` it.

  ## Example

  https://github.com/user-attachments/assets/55f716b4-74d6-4e80-ac30-0165235505cd
