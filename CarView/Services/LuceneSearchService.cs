using CarView.Models;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace CarView.Services
{
    public class LuceneSearchService
    {
        private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        private readonly string _indexPath = Path.Combine(Environment.CurrentDirectory, "LuceneIndex");

        public void BuildIndex(IEnumerable<Car> cars)
        {
            using var dir = FSDirectory.Open(_indexPath);
            var analyzer = new StandardAnalyzer(AppLuceneVersion);
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer)
            {
                OpenMode = OpenMode.CREATE 
            };

            using var writer = new IndexWriter(dir, indexConfig);

            foreach (var car in cars)
            {
                var doc = new Document
                {
                    new StringField("Id", car.Id.ToString(), Field.Store.YES),
                    new TextField("ModelName", car.ModelName ?? "", Field.Store.YES),
                    new TextField("Description", car.Description ?? "", Field.Store.YES),
                    new TextField("BrandName", car.Brand?.Name ?? "", Field.Store.YES)
                };
                writer.AddDocument(doc);
            }
            writer.Commit();
        }

        public IEnumerable<int> Search(string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery)) return Array.Empty<int>();

            using var dir = FSDirectory.Open(_indexPath);
            if (!DirectoryReader.IndexExists(dir)) return Array.Empty<int>();

            using var reader = DirectoryReader.Open(dir);
            var searcher = new IndexSearcher(reader);
            var analyzer = new StandardAnalyzer(AppLuceneVersion);

         
            var parser = new MultiFieldQueryParser(AppLuceneVersion, new[] { "ModelName", "Description", "BrandName" }, analyzer);

            
            var query = parser.Parse(searchQuery.Trim() + "*");

            var hits = searcher.Search(query, 10).ScoreDocs; 

            var results = new List<int>();
            foreach (var hit in hits)
            {
                var foundDoc = searcher.Doc(hit.Doc);
                results.Add(int.Parse(foundDoc.Get("Id")));
            }
            return results;
        }
    }
}
