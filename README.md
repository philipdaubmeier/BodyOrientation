BodyOrientation
===============

This project arose from my Master's Thesis in Computer Science and provides a 
library and the GUI components for training a machine learning algorithm to infer
the persons body position only from sensor readings of a mobile phone device, carried 
in the users trousers pocket. The method implemented in this project uses supervised learning, 
with data from a Kinect depth sensor taken as the ground truth to train the algorithm on.

Around this central goal, a lot of useful functions and GUI components are implemented
in this project to collect the data from all sensors in real-time and merge them into
a single stream to process, filter and transform further before finally feeding it into
the machine learning algorithm. For the actual learner, the R statistical language and
its libraries are used, together with a .NET wrapper to be able to access it.

All raw sensor values as well as the processed features are observable by visualizing
GUI components, such as live plotters and 3D views (for the mobile phones attitude and
the users skeleton, delivered by the Kinect SDK framework). The data can be recorded
and be played again later to try different methods on the same set of training data.

### Building

The project is written in C# and uses the .NET framework and WPF as GUI toolkit. 
The target framework version is currently .NET 4, but that is not written in stone. The binaries 
are easiest built with Visual Studio 2010 using the existing solution file.

### Dependencies
  * R.NET wrapper: [rdotnet.codeplex.com](http://rdotnet.codeplex.com)
  * R library: [r-project.org](http://www.r-project.org)

### Mobile Application

Without being able to read the sensor values of the mobile phone, the application is basically
useless. So, one of the key components is the mobile application (code not contained in this repo):

The app 'Sensor Emitter', to collect the phones sensor values and transmit 
them onto the PC is also written in C# and available for the Windows Phone 7 OS in the marketplace:
[Sensor Emitter App](http://windowsphone.com/s?appid=08c94bea-924b-44b9-b4e3-03e571ea8ceb)

More infos about the mobile application can also be found here:
[Sensor Emitter Website](http://www.daubmeier.de/philip/sensoremitter)

