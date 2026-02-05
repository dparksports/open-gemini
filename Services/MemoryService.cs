using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenClaw.Windows.Models;
using OpenClaw.Windows.Services.Data;

namespace OpenClaw.Windows.Services
{
    public class MemoryService
    {
        private readonly ChatContextDb _db;
        private readonly EmbeddingService _embeddingService;

        public MemoryService(ChatContextDb db, EmbeddingService embeddingService)
        {
            _db = db;
            _embeddingService = embeddingService;
        }

        public async Task SaveMemoryAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            try
            {
                var vector = await _embeddingService.GetEmbeddingAsync(content);
                if (vector.Length > 0)
                {
                    await _db.SaveMemoryAsync(content, vector);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving memory: {ex.Message}");
            }
        }

        public async Task<List<MemoryItem>> SearchMemoriesAsync(string query, int limit = 3, float threshold = 0.6f)
        {
            try
            {
                var queryVector = await _embeddingService.GetEmbeddingAsync(query);
                if (queryVector.Length == 0) return new List<MemoryItem>();

                // Naive approach: Fetch all and compute cosine similarity in-memory
                // For < 10k items, this is virtually instant.
                var allMemories = await _db.GetAllMemoriesAsync();

                var results = allMemories
                    .Select(m => new { Memory = m, Score = CosineSimilarity(queryVector, m.Vector) })
                    .Where(x => x.Score >= threshold)
                    .OrderByDescending(x => x.Score)
                    .Take(limit)
                    .Select(x => x.Memory)
                    .ToList();

                return results;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching memories: {ex.Message}");
                return new List<MemoryItem>();
            }
        }

        private float CosineSimilarity(float[] vecA, float[] vecB)
        {
            if (vecA.Length != vecB.Length) return 0f;

            float dotProduct = 0f;
            float magnitudeA = 0f;
            float magnitudeB = 0f;

            for (int i = 0; i < vecA.Length; i++)
            {
                dotProduct += vecA[i] * vecB[i];
                magnitudeA += vecA[i] * vecA[i];
                magnitudeB += vecB[i] * vecB[i];
            }

            if (magnitudeA == 0 || magnitudeB == 0) return 0f;

            return dotProduct / ((float)Math.Sqrt(magnitudeA) * (float)Math.Sqrt(magnitudeB));
        }
    }
}
