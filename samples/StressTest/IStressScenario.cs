namespace StressTest;

public interface IStressScenario
{
    string Name { get; }
    string Description { get; }
    void Setup(StressWindow window, Lumi.Core.Element container);
    void Update(int frameNumber);
}
