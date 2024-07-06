namespace UnityEngine.InputSystem.PS5.ControllerSample
{
    public static class MathUtils
    {
        /// <summary>
        /// Remap a float value from a range (from1 -> from2) to another range (to1 -> to2)
        /// </summary>
        public static float Remap (this float value, float from1, float to1, float from2, float to2) {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }
}
