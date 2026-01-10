using UnityEngine;
using System.Collections.Generic;

namespace WitShells.WitPose.Editor
{
    /// <summary>
    /// Shared utilities for WitPose Editor.
    /// Handles string manipulation for bone names and muscle display.
    /// </summary>
    public static class WitPoseUtils
    {
        public static string GetMuscleEmoji(string muscleName)
        {
            if (string.IsNullOrEmpty(muscleName)) return "🤷";

            string lowerName = muscleName.ToLower();

            // Spine and torso
            if (lowerName.Contains("spine")) return "🦴";
            if (lowerName.Contains("chest")) return "💪";
            if (lowerName.Contains("upper chest")) return "🫀";

            // Head and neck
            if (lowerName.Contains("neck")) return "🦒";
            if (lowerName.Contains("head")) return "🗣️";
            if (lowerName.Contains("jaw")) return "🦷";
            if (lowerName.Contains("eye")) return "👁️";

            // Arms and hands
            if (lowerName.Contains("shoulder")) return "💪";
            if (lowerName.Contains("arm") || lowerName.Contains("forearm")) return "🦾";
            if (lowerName.Contains("hand")) return "✋";
            if (lowerName.Contains("thumb")) return "👍";
            if (lowerName.Contains("index")) return "☝️";
            if (lowerName.Contains("middle")) return "🖕";
            if (lowerName.Contains("ring")) return "💍";
            if (lowerName.Contains("little")) return "🤙";

            // Legs and feet
            if (lowerName.Contains("upper leg") || lowerName.Contains("thigh")) return "🦵";
            if (lowerName.Contains("lower leg") || lowerName.Contains("calf")) return "🦵";
            if (lowerName.Contains("foot")) return "🦶";
            if (lowerName.Contains("toe")) return "🦶";

            // Generic body parts
            if (lowerName.Contains("left")) return "⬅️";
            if (lowerName.Contains("right")) return "➡️";
            if (lowerName.Contains("front")) return "⬆️";
            if (lowerName.Contains("back")) return "⬇️";

            return "⚡"; // Default for any other muscle
        }

        public static string CleanMuscleName(string muscleName)
        {
            if (string.IsNullOrEmpty(muscleName)) return "Unknown";

            // Remove redundant parts and make more readable
            string cleaned = muscleName;

            // Replace common patterns
            cleaned = cleaned.Replace("LeftHand.", "L. Hand ");
            cleaned = cleaned.Replace("RightHand.", "R. Hand ");
            cleaned = cleaned.Replace("Left", "L.");
            cleaned = cleaned.Replace("Right", "R.");
            cleaned = cleaned.Replace("Upper", "Up.");
            cleaned = cleaned.Replace("Lower", "Low.");
            cleaned = cleaned.Replace("Front", "Frt.");
            cleaned = cleaned.Replace("Back", "Bck.");
            cleaned = cleaned.Replace("Twist", "Twst");
            cleaned = cleaned.Replace("Stretch", "Strch");

            // Capitalize first letter of each word
            string[] words = cleaned.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }

            return string.Join(" ", words);
        }

        public static string CleanBoneName(string boneName)
        {
            if (string.IsNullOrEmpty(boneName)) return boneName;

            // Remove common suffixes and prefixes
            string cleaned = boneName;

            // Remove _CTRL suffix
            if (cleaned.EndsWith("_CTRL"))
                cleaned = cleaned.Substring(0, cleaned.Length - 5);

            // Remove _Bone suffix
            if (cleaned.EndsWith("_Bone"))
                cleaned = cleaned.Substring(0, cleaned.Length - 5);

            // Remove common prefixes
            if (cleaned.StartsWith("mixamorig:"))
                cleaned = cleaned.Substring(10);

            return cleaned;
        }
    }
}