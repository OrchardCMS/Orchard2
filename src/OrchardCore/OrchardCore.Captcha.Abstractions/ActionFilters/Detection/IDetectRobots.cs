namespace OrchardCore.Captcha.ActionFilters.Detection
{
    /// <summary>
    /// This interface describes the contract for components that can detect Robots.
    /// </summary>
    public interface IDetectRobots
    {
        /// <summary>
        /// Performs a check to see if the current request could be submitted by a robot
        /// </summary>
        /// <returns>Detection result</returns>
        RobotDetectionResult DetectRobot();

        /// <summary>
        /// Clear the detectors internal state, we are not dealing with a robot
        /// </summary>
        void IsNotARobot();

        /// <summary>
        /// We are dealing with a robot, shields up
        /// </summary>
        void FlagAsRobot();
    }

    public class RobotDetectionResult
    {
        public bool IsRobot { get; set; }
    }
}
