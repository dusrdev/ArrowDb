namespace ArrowDbCore.Tests.Unit.Isolated;

public class StaticVariables {
    [Fact]
    public async Task Instance_Ids_Match_Running() {
        // At startup of process instances should be 0
        Assert.Equal(0, ArrowDb.RunningInstances);
        // Create 10 instances and check counter
        const int count = 10;
        var dbs = new ArrowDb[count];
        for (var i = 0; i < count; i++) {
            dbs[i] = await ArrowDb.CreateInMemory();
        }
        Assert.Equal(count, ArrowDb.RunningInstances);
    }
}
