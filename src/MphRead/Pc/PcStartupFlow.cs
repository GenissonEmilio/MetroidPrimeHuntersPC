using System;

namespace MphRead
{
    public enum PcStartupPage { Opening, Title, Files, Menu }

    // No rendering or file writes: one input advances exactly one page.
    public sealed class PcStartupFlow
    {
        public PcStartupPage Page { get; private set; }
        public byte SaveSlot { get; private set; }
        public PcStartupFlow(bool opening) => Page = opening ? PcStartupPage.Opening : PcStartupPage.Title;
        public void FinishOpening()
        {
            if (Page == PcStartupPage.Opening) Page = PcStartupPage.Title;
        }
        public void PressTitle()
        {
            if (Page == PcStartupPage.Title) Page = PcStartupPage.Files;
        }
        public void SelectFile(byte slot)
        {
            if (slot is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(slot));
            if (Page != PcStartupPage.Files) return;
            SaveSlot = slot;
            Page = PcStartupPage.Menu;
        }
        public void Back()
        {
            if (Page == PcStartupPage.Menu) Page = PcStartupPage.Files;
            else if (Page == PcStartupPage.Files) Page = PcStartupPage.Title;
        }
    }

    public enum MovieClockStep { Wait, Advance, Complete }

    // A late decoder must never turn elapsed wall time into skipped story frames.
    public sealed class MoviePlaybackClock
    {
        public int FrameIndex { get; private set; }
        public int FrameCount { get; }
        public double FrameDuration { get; }
        private double _deadline;
        public MoviePlaybackClock(int frames, double fps)
        {
            if (frames < 1 || !Double.IsFinite(fps) || fps <= 0) throw new ArgumentOutOfRangeException();
            FrameCount = frames;
            FrameDuration = 1 / fps;
            _deadline = FrameDuration;
        }
        public MovieClockStep Step(double elapsed, bool nextFrameReady)
        {
            if (elapsed < _deadline) return MovieClockStep.Wait;
            if (FrameIndex == FrameCount - 1) return MovieClockStep.Complete;
            if (!nextFrameReady) return MovieClockStep.Wait;
            FrameIndex++;
            _deadline = Math.Max(_deadline + FrameDuration, elapsed);
            return MovieClockStep.Advance;
        }
    }
}
