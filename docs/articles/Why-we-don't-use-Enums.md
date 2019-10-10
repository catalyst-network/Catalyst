We don't use Enums in this codebase here are a few reasons why

## 1) They're not considered type safe.

Taken from the blog article [Enums Are Evil](https://www.planetgeek.ch/2009/07/01/enums-are-evil)

> If you think enum’s are type safe, you will get into deep trouble. Look at the example code below. The enum that we have defined is 1, 2 and 3. Right?
Now try to call

```
new EnumTricks.IsVolumeHigh((Volume)27);
```

> That should fail, at least at runtime. It didn’t? That’s really strange… The wrong call will not be detected during compilation nor during runtime. You feel yourself in a false safety.

```
public enum Volume
{
  Low = 1,
  Medium,
  High
}
 
public class EnumTricks
{
 
  public bool IsVolumeHigh(Volume volume)
  {
    var result = false;
 
    switch (volume)
    {
      case Volume.Low:
        Console.WriteLine("Volume is low.");
        break;
 
      case Volume.Medium:
        Console.WriteLine("Volume is medium.");
        break;
 
      case Volume.High:
        Console.WriteLine("Volume is high.");
        result = true;
        break;
    }
 
    return result;
  }
}
```

## 2) Difficult to convert

Taken from the blog article [Enums Are Evil](https://www.planetgeek.ch/2009/07/01/enums-are-evil)

>Have you ever tried to convert from enum to int, int to enum, string to enum, string to int value of enum? I’ll show you here with the Volume enum from above, but I didn’t check for the:

```
public int EnumToInt(Volume volume)
{
  return (int)volume;
}
 
public Volume IntToEnum(int intValue)
{
  return (Volume)intValue;
}
 
public Volume StringToEnum(string stringValue)
{
  return (Volume)Enum.Parse(typeof(Volume), stringValue);
}
 
public int StringToInt(string stringValue)
{
  var volume = StringToEnum(stringValue);
  return EnumToInt(volume);
}
```

### When Can I Use Enums?

You don't.

For consistency we have a defined convention on how to use "TypeSafe Enums", and this should always be followed. If you are partaking in a code review and spot the use of enums you should automatically deny the PR and point the author of the PR to this Wiki. Nearly all code can be like for like refactored to replace enums and create a class extending from 

```
Enumeration : IEquatable<Enumeration>
```

Code control flow with switch statements and enums can easily converted from bad enums like this

```
            switch (messageType)
            {
                case DtoMessageType.Ask:
                    return BuildAskMessage(messageDto);
                case DtoMessageType.Tell:
                    return BuildTellMessage(messageDto, correlationId);
                default:
                    throw new ArgumentException();
            }
```

to if/ else statements like this

```
            if (messageType == MessageTypes.Ask)
            {
                return BuildAskMessage(messageDto);
            }

            if (messageType == MessageTypes.Tell)
            {
                return BuildTellMessage(messageDto, correlationId);   
            }
```

### I'm not convinced

You don't have to be, everyone has their own preference. But as stated above a convention has been set for this project with more pro's than cons. Here is some further reading on the subject

> WARNING As a rule of thumb, enums are code smells and should be refactored to polymorphic classes. [8] Seemann, Mark, Dependency Injection in .Net, 2011, p. 342
* http://sd.blackball.lv/library/Dependency_Injection_in_.NET_(2011).pdf

> [8] Martin Fowler et al., Refactoring: Improving the Design of Existing Code (New York: Addison-Wesley, 1999), 82.
* https://www.csie.ntu.edu.tw/~r95004/Refactoring_improving_the_design_of_existing_code.pdf

* https://codeblog.jonskeet.uk/2006/01/05/classenum/
