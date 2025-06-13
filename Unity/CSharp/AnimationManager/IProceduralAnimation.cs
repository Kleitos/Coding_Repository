public interface IProceduralAnimation
{
    string AnimationID { get; }
    bool IsPlaying { get; }
    void Play();
    void Stop();
}
