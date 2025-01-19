namespace ArrowDbCore.Tests.Unit;

public class OnChange {
    [Fact]
    public async Task OnChange_Upserts_Shows_Expected_Change() {
        var db = await ArrowDb.CreateInMemory();
        var change = (ArrowDbChangeType)(-1); // invalid state to ensure the event is triggered
        db.OnChange += (_, args) => {
            change = args.ChangeType;
        };
        db.Upsert("1", 1, JContext.Default.Int32);
        Assert.Equal(ArrowDbChangeType.Upsert, change);
    }
    [Fact]
    public async Task OnChange_Remove_Shows_Expected_Change() {
        var db = await ArrowDb.CreateInMemory();
        db.Upsert("1", 1, JContext.Default.Int32);
        var change = (ArrowDbChangeType)(-1); // invalid state to ensure the event is triggered
        db.OnChange += (_, args) => {
            change = args.ChangeType;
        };
        db.TryRemove("1");
        Assert.Equal(ArrowDbChangeType.Remove, change);
    }
    [Fact]
    public async Task OnChange_Clear_Shows_Expected_Change() {
        var db = await ArrowDb.CreateInMemory();
        db.Upsert("1", 1, JContext.Default.Int32);
        var change = (ArrowDbChangeType)(-1); // invalid state to ensure the event is triggered
        db.OnChange += (_, args) => {
            change = args.ChangeType;
        };
        db.Clear();
        Assert.Equal(ArrowDbChangeType.Clear, change);
    }
}