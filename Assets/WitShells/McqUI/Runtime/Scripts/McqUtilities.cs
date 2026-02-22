using System.Collections.Generic;
using UnityEngine;

namespace WitShells.McqUI
{
    /// <summary>
    /// Utility class for creating and managing MCQ data sets
    /// </summary>
    public static class McqUtilities
    {
        /// <summary>
        /// Create a simple MCQ with one correct and multiple wrong answers
        /// </summary>
        public static McqData CreateSimpleMcq(string question, string correctAnswer, params string[] wrongAnswers)
        {
            return new McqData(question, correctAnswer, wrongAnswers);
        }

        /// <summary>
        /// Create an MCQ with time limit
        /// </summary>
        public static McqData CreateTimedMcq(string question, string correctAnswer, float timeLimit, params string[] wrongAnswers)
        {
            return new McqData(question, correctAnswer, wrongAnswers, null, timeLimit);
        }

        /// <summary>
        /// Create multiple MCQs from parallel arrays
        /// </summary>
        public static List<McqData> CreateMcqSet(string[] questions, string[] correctAnswers, string[][] wrongAnswersSet)
        {
            var mcqList = new List<McqData>();

            int count = Mathf.Min(questions.Length, correctAnswers.Length);
            count = Mathf.Min(count, wrongAnswersSet.Length);

            for (int i = 0; i < count; i++)
            {
                mcqList.Add(new McqData(questions[i], correctAnswers[i], wrongAnswersSet[i]));
            }

            return mcqList;
        }

        /// <summary>
        /// Shuffle a list of MCQ data
        /// </summary>
        public static void ShuffleMcqList(List<McqData> mcqList)
        {
            for (int i = 0; i < mcqList.Count; i++)
            {
                var randomIndex = Random.Range(i, mcqList.Count);
                (mcqList[i], mcqList[randomIndex]) = (mcqList[randomIndex], mcqList[i]);
            }
        }

        /// <summary>
        /// Get a random subset of MCQs from a larger set
        /// </summary>
        public static List<McqData> GetRandomSubset(List<McqData> mcqList, int count)
        {
            if (count >= mcqList.Count)
                return new List<McqData>(mcqList);

            var shuffled = new List<McqData>(mcqList);
            ShuffleMcqList(shuffled);

            return shuffled.GetRange(0, count);
        }

        /// <summary>
        /// Validate a list of MCQ data
        /// </summary>
        public static bool ValidateMcqSet(List<McqData> mcqList, out string errorMessage)
        {
            errorMessage = "";

            if (mcqList == null || mcqList.Count == 0)
            {
                errorMessage = "MCQ list is null or empty";
                return false;
            }

            for (int i = 0; i < mcqList.Count; i++)
            {
                if (!mcqList[i].IsValid())
                {
                    errorMessage = $"MCQ at index {i} is invalid";
                    return false;
                }
            }

            return true;
        }
    }
}