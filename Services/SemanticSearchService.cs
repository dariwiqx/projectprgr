using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace прпгр.Services
{
    public class SemanticSearchService
    {
        private readonly MaterialService _store;
        private Dictionary<int, Dictionary<string, double>> _tfidfVectors = new();
        private Dictionary<string, double> _idf = new();
        private bool _indexBuilt = false;

        public SemanticSearchService(MaterialService store)
        {
            _store = store;
        }

        public void RebuildIndex()
        {
            var materials = _store.GetAll().Where(m => m.Status == "Approved").ToList();
            var documents = new Dictionary<int, List<string>>();

            foreach (var m in materials)
            {
                var text = string.Join(" ",
                    m.Title ?? "",
                    m.Description ?? "",
                    m.Topic ?? "",
                    string.Join(" ", _store.GetTagsForMaterial(m.Id).Select(t => t.Name)));
                documents[m.Id] = Tokenize(text);
            }

            // Calculate IDF
            _idf.Clear();
            int totalDocs = documents.Count;
            if (totalDocs == 0) { _indexBuilt = true; return; }

            var termDocCount = new Dictionary<string, int>();
            foreach (var doc in documents.Values)
            {
                foreach (var term in doc.Distinct())
                {
                    if (!termDocCount.ContainsKey(term))
                        termDocCount[term] = 0;
                    termDocCount[term]++;
                }
            }

            foreach (var kvp in termDocCount)
            {
                _idf[kvp.Key] = Math.Log((double)totalDocs / (1 + kvp.Value));
            }

            // Calculate TF-IDF vectors
            _tfidfVectors.Clear();
            foreach (var kvp in documents)
            {
                var tokens = kvp.Value;
                var totalTokens = tokens.Count;
                if (totalTokens == 0) continue;

                var tf = new Dictionary<string, double>();
                foreach (var token in tokens)
                {
                    if (!tf.ContainsKey(token))
                        tf[token] = 0;
                    tf[token]++;
                }

                var tfidf = new Dictionary<string, double>();
                foreach (var t in tf)
                {
                    var idf = _idf.ContainsKey(t.Key) ? _idf[t.Key] : 0;
                    tfidf[t.Key] = (t.Value / totalTokens) * idf;
                }
                _tfidfVectors[kvp.Key] = tfidf;
            }

            _indexBuilt = true;
        }

        public List<(int MaterialId, double Score)> Search(string query, int topN = 20)
        {
            if (!_indexBuilt) RebuildIndex();
            if (_tfidfVectors.Count == 0) return new List<(int, double)>();

            var queryTokens = Tokenize(query);
            if (queryTokens.Count == 0) return new List<(int, double)>();

            var queryTf = new Dictionary<string, double>();
            foreach (var token in queryTokens)
            {
                if (!queryTf.ContainsKey(token))
                    queryTf[token] = 0;
                queryTf[token]++;
            }

            var queryVector = new Dictionary<string, double>();
            foreach (var t in queryTf)
            {
                var idf = _idf.ContainsKey(t.Key) ? _idf[t.Key] : 0;
                queryVector[t.Key] = (t.Value / queryTokens.Count) * idf;
            }

            var results = new List<(int MaterialId, double Score)>();
            foreach (var docVec in _tfidfVectors)
            {
                var similarity = CosineSimilarity(queryVector, docVec.Value);
                if (similarity > 0.001)
                {
                    results.Add((docVec.Key, similarity));
                }
            }

            return results.OrderByDescending(r => r.Score).Take(topN).ToList();
        }

        private static List<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            text = text.ToLowerInvariant();
            text = Regex.Replace(text, @"[^\w\s]", " ");
            return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                       .Where(t => t.Length > 1)
                       .ToList();
        }

        private static double CosineSimilarity(Dictionary<string, double> a, Dictionary<string, double> b)
        {
            double dotProduct = 0;
            double normA = 0;
            double normB = 0;

            foreach (var kvp in a)
            {
                normA += kvp.Value * kvp.Value;
                if (b.TryGetValue(kvp.Key, out var bVal))
                    dotProduct += kvp.Value * bVal;
            }

            foreach (var kvp in b)
                normB += kvp.Value * kvp.Value;

            if (normA == 0 || normB == 0) return 0;
            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }
    }
}
