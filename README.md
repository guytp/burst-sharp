# burst-sharp
burst-sharp is a suite of tools for Burstcoin written using .Net Core 2.0.0.  The idea is to create a full suite of easy-to-use tools for Burstcoin that function across platforms and processor architectures.

Currently burst-sharp should be considered a in early prototype stages and the only application ready to test is the miner.

Donations are welcome to BURST-DXGM-62RW-X8G6-A493G or BTC to 1HmJjK9nNvEstQNLBfxJyvTfkgGBCmngHZ.


## Pre-Requisites
You must have .Net Core 2.0.0 installed.  This runs in Windows, Linux and macOS.  You can get instructions for your platform from https://www.microsoft.com/net/core/preview

If you want to run this on a Raspberry Pi I've created full step-by-step instructions at https://guyrobottv.wordpress.com/2017/06/23/installing-net-core-2-on-a-raspberry-pi/ including a video to run you through how to get .Net Core and burst-sharp up and running.


## Miner
The miner is the most complete piece of this suite.  There are some current issues:

1. It will only mine optimized plots
2. Solo mining is not supported
3. Speed could be increased somewhat - although I can still complete rounds in a decent time and this is on the list to improve


### Installation
It should be super-easy to install:
* Ensure you have the .Net Core 2.0.0 installed as above
* Extract the ZIP file to a folder of your choice
* Edit the configuratation (defined below)
* Open a command prompt or terminal window in the same folder as you extracted the ZIP file and run:
```bash
dotnet Miner.dll
```
* The miner will then start and show its user interface.  As deadlines are calculated and submitted to the pool they appear on the right and current status text on the left.
* Press F10 when you want to terminate the miner


### Configuration
Once you've extracted your ZIP file there is a file called miner-config.json.  You will need to change settings as below.

You definitely want to change PlotDirectories and PoolApiUrl.  Just to get going the rest can probably be left as-is.

| Setting | Details |
|:--------|--------:|
| **PlotDirectories** | A list of paths in quotes separated by spaces - this is where your plot files are (i.e. "D:\\Plots", "C:\\" or "/mnt/Disk1", "/mnt/Disk2" depending on your platform).  You must use two backslashes instead of one (i.e. \\ instead of \) for paths as a single \ is considered a special character. |
| **PoolApiUrl** | This is the address that you use to communication between your miner and pool - for example http://mypool.com:8124. |
| **ThreadCountPlotChecker** | The maximum number of threads that you want to use to read plots.  This should be equal to the number of logical CPU cores (including hyper threads) that you have.  The PlotChecker is responsible for taking the plots read from disk and validating deadlines so the more the better but don't have more than you do CPU cores as this won't create any benefit. |
| **MemoryLimitPerReader** | This is used as a maximum allowed memory (in megabytes) size that is read from disk.  It's very unlikely this much will be used but if you want to be on the safe side take half your RAM and divide it by the number of disks that you have and round it.  For example 8GB RAM with 10 disks would give 410 here (8192 / 2 / 10).  The default is probably fine unless you experience problems. |
| **MemoryLimitPlotChecker** | This indicates how much memory you want to give to the plot checker.  The default should be fine and much like the limit per reader it is incredibly unlikely to reach this level but you could calculate half your RAM and divide it by the number you set in ThreadCountPlotChecker - for example 8GB of RAM with 4 plot checkers would be 1024 (8192/2/4). |
| **LogInfo** | Whether or not to display useful messages - the default is probably fine (true or false). |
| **LogWarn** | Whether or not to display messages that could indicate some kind of problem - the default is probably fine (true or false). |
| **LogError** | Whether or not to display messages that definitely indicate some kind of problem - the default is probably fine (true or false). |
| **LogDebug** | Whether or not to display much more detailed messages including a file-by-file breakdown of read speeds to show further progress (true or false). |


### Screenshots
![Miner Screenshot](https://image.ibb.co/jqizhk/Screenshot_from_2017_06_29_10_40_39.png)


## Plotter
The source code for this is available but as yet no releases.  The plotter generates only optimised plots - however it currently is incredibly slow (I can get around 900 plots per minute on a system that can get 6,000 with xplotter).  Once the Miner is more complete I'll spend some time on this.


## Other Tools
I'm considering working on GUIs for these, potentially new pool software (that can use distributed Raspberry Pis as validation nodes to provide a low-cost and low-energy way to run pools) and some optimization tools.  I'd like to be led by feedback on what people actually want though rather than just doing this for fun.


## Build Status
Builds are automatically generated whenever a new commit is merged in to master.  Successful builds result in a new release automatically being generated and the code tagged to match that version.  Any builds from a branch other than master should be considered pre-release.

|    | Master (Release) |
|:---|-----------------:|
|**Miner**|![dll-release](https://guytp.visualstudio.com/_apis/public/build/definitions/a4d5b068-0942-4ac6-a43a-5dd4374ff718/25/badge)|


## Contact / Feature Requests
You are welcome to contact me by e-mail (guy at guytp.org) or if you have a specific issue feel free to open it here in Github and tag @guytp.  I'd love to know what you think.