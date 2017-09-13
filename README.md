# Dexih.Utils.RealTimeBuffer

[build]:    https://ci.appveyor.com/project/dataexperts/dexih-utils-realtimebuffer
[build-img]: https://ci.appveyor.com/api/projects/status/xa7ta52icka2wmgk?svg=true
[nuget]:     https://www.nuget.org/packages/Dexih.Utils.RealTimeBuffer
[nuget-img]: https://badge.fury.io/nu/Dexih.Utils.RealTimeBuffer.svg
[nuget-name]: Dexih.Utils.RealTimeBuffer

[![Build status][build-img]][build] [![Nuget][nuget-img]][nuget]


The realtime buffer is a push/pop buffer that allows one thread to push data to another thread via an asynchronous buffer.   The buffer can be restricted in size to keep memory and resource usage.  When the buffer is full, and 'push' thread will wait, and when the buffer is empty the 'pop' thread will wait until some data appears.  

This buffer is useful in scenarious such as a file uploads and database downloads where resource and memory management is important.

---

### Installation

Add the latest version of the package "Dexih.Utils.RealTimeBuffer" to a .net core/.net project.

---

### Usage

It is recommended that a single thread performs the `push` and another single thread performs the `pop` operation.  The library is thread safe in this scenario.  

The library is NOT thread safe, when multiple threads perform `push` or `pull` simultaneously, and this will have unexpected results.

To get started, add the following name space.
```csharp
using Dexih.Utils.RealTimeBuffer;
```

To create a new shared buffer that can store 10 items simultaneously and has a push/pull timeout of 5000ms.

```csharp
var buffer = new RealTimeBuffer<string>(10, 5000);
```

To push some data to the buffer.  Note, if the buffer is full, the `await` will pause the push thread, until an item is popped from the buffer, or a timeout / cancel occurs.
```csharp
var data = "my data...";
await buffer.push(data, CancellationToken.None);
```

To push some data to the buffer.  Note, if the buffer is full, the `await` will pause, until an item is removed from the buffer, or a timeout / cancel occurs.
```csharp
var data = "my data...";
await buffer.push(data);
```

To get data from buffer.  If the buffer is empty, the `await` will pause, until an item is removed from the buffer, or a timeout or cancel occurs.
The pop returns structure containing the value (Package propety) and the status (Status property.
```csharp
var recieved = await buffer.pop();
var data = recieved.Package; 
var status = received.Status;
```

To mark the `push` process complete, include a `true` flag in the `isFinal` parameter.
```csharp
var data = "last bit of data...";
await buffer.push(data, true);
```

If the buffer has been marked finished, the `pop` will throw a `RealTimeBufferFinishedException` if more pops are attempted on the empty buffer.  To avoid an exception, the `Status` property of the buffer package can be checked.

```csharp
while(true)
{
    var recieved = await buffer.pop();
    
    if(received.Status == ERealTimeBufferStatus.Complete)
    {
        break;
    }
}
```


