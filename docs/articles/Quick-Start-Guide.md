This is a quick start guide for new and existing .Net developers

## 1. Install .Net

Catalyst.Node works with .Net Core v2.2. You'll need to have the .Net SDK installed. 

If you don't have .Net Core installed you can follow the instructions for your platform bellow.

- [Windows](https://dotnet.microsoft.com/download?initial-os=windows)
- [Linux](https://dotnet.microsoft.com/download?initial-os=linux)
- [macOS](https://dotnet.microsoft.com/download?initial-os=macos)


## 2. Clone the repository

To clone the repository it is assumed you have git installed
If not the follow the [git install instructions](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git) for Linux/Windows/macOS

The clone command is

`git clone git@github.com:catalyst-network/Catalyst.Node.git `

## 3. Clone git sub-modules

We utilize git sub-modules to for certain dependencies. You will need these to build the code base. To grab them.

First navigate to the Catalyst.Node folder

`cd Catalyst.Node`

The initialize the sub-modules by running

`git submodule init`

The clone then by running

`git submodule update`

## 4. Install nuget dependencies

Now we have our code and sub-modules, the next step is to install the dependencies from .Net's package manager Nuget.

Navigate to the src folder

`cd src`

Next restore the dependencies

`dotnet restore`

## 5. Build the solution

As a diligent reader of installation instructions, building the solution should be no issues.

`dotnet build`

## 6. Run the test suite

To check all is good under the hood, you can run the test suite, and if we are diligent developers there should be no issues. If you're on linux you need to [Create a Self Signed Certificate](https://github.com/catalyst-network/Catalyst.Node/wiki/Create-a-Self-Signed-Certificate)

`dotnet test`

## 7. Lambo, Moon

You should now have a lambo that will take you to the moon.