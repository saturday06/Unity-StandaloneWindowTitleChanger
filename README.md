# StandaloneWindowTitleChanger

[![Build Status](https://dev.azure.com/saturday06stripe/Unity-StandaloneWindowTitleChanger/_apis/build/status/saturday06.Unity-StandaloneWindowTitleChanger?branchName=master)](https://dev.azure.com/saturday06stripe/Unity-StandaloneWindowTitleChanger/_build/latest?definitionId=2&branchName=master)


```csharp
try
{
    StandaloneWindowTitle.Change("Happy Hacking");
}
catch (StandaloneWindowTitleChangeException e)
{
    switch (e.Cause)
    {
        case StandaloneWindowTitleChangeException.Error.NoWindow:
            break;
        case StandaloneWindowTitleChangeException.Error.Unknown:
            Debug.LogException(e);
            break;
        default:
            Debug.LogException(e);
            break;
    }
}
```
