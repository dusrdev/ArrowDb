namespace ArrowDbCore.Tests.Unit.Isolated;

// These tests need to be separated into different class to ensure they run on different processes

public class StaticVariables1 {
    [Fact]
    public void Instance_Counter_Is_Zero_At_Startup() {
        Assert.Equal(0, ArrowDb.RunningInstances);
    }
}

public class StaticVariables2 {
    [Fact]
    public async Task Instance_Ids_Match_Running() {
        const int count = 10;
        var dbs = new ArrowDb[count];
        for (var i = 0; i < count; i++) {
            dbs[i] = await ArrowDb.CreateInMemory();
        }
        Assert.Equal(count, ArrowDb.RunningInstances);
    }
}
