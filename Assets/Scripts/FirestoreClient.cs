using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WildlifeAdventure
{
    /// <summary>
    /// Reads and writes Cloud Firestore documents over its REST API. We keep
    /// every stored field a flat scalar (string / integer), which keeps the
    /// typed-JSON encoding simple and makes the data readable in the Firebase
    /// console (good for report screenshots).
    /// </summary>
    public static class FirestoreClient
    {
        static string Key => "?key=" + FirebaseConfig.ApiKey;

        // ---------- Reads ----------

        /// <summary>GET a single document; returns its fields as name -> scalar string.</summary>
        public static IEnumerator GetDocument(string path, Action<bool, Dictionary<string, string>> done)
        {
            string url = FirebaseConfig.FirestoreBase + "/" + path + Key;
            yield return RestClient.Send("GET", url, null, FirebaseAuth.IdToken, (ok, code, resp) =>
            {
                if (!ok) { done(false, null); return; }
                var root = MiniJson.Parse(resp);
                var fields = ExtractFields(MiniJson.Get(root, "fields"));
                done(true, fields);
            });
        }

        /// <summary>GET every document in a collection; returns a list of field maps.</summary>
        public static IEnumerator ListCollection(string collection,
                                                 Action<bool, List<Dictionary<string, string>>> done)
        {
            string url = FirebaseConfig.FirestoreBase + "/" + collection + Key + "&pageSize=300";
            yield return RestClient.Send("GET", url, null, FirebaseAuth.IdToken, (ok, code, resp) =>
            {
                if (!ok) { done(false, null); return; }
                var root = MiniJson.Parse(resp);
                var docs = MiniJson.Arr(MiniJson.Get(root, "documents"));
                var list = new List<Dictionary<string, string>>();
                if (docs != null)
                    foreach (var d in docs)
                        list.Add(ExtractFields(MiniJson.Get(d, "fields")));
                done(true, list);
            });
        }

        // ---------- Writes ----------

        /// <summary>Creates or overwrites a document at a known id.</summary>
        public static IEnumerator PatchDocument(string path, Dictionary<string, object> values,
                                                Action<bool> done)
        {
            string url = FirebaseConfig.FirestoreBase + "/" + path + Key;
            string body = EncodeFields(values);
            yield return RestClient.Send("PATCH", url, body, FirebaseAuth.IdToken,
                (ok, code, resp) => done(ok));
        }

        /// <summary>Creates a new document with an auto-generated id.</summary>
        public static IEnumerator CreateDocument(string collection, Dictionary<string, object> values,
                                                 Action<bool> done)
        {
            string url = FirebaseConfig.FirestoreBase + "/" + collection + Key;
            string body = EncodeFields(values);
            yield return RestClient.Send("POST", url, body, FirebaseAuth.IdToken,
                (ok, code, resp) => done(ok));
        }

        // ---------- Encoding / decoding ----------

        static string EncodeFields(Dictionary<string, object> values)
        {
            var sb = new StringBuilder();
            sb.Append("{\"fields\":{");
            bool first = true;
            foreach (var kv in values)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(MiniJson.Escape(kv.Key)).Append("\":");
                sb.Append(EncodeValue(kv.Value));
            }
            sb.Append("}}");
            return sb.ToString();
        }

        static string EncodeValue(object v)
        {
            if (v is int || v is long)
                return "{\"integerValue\":\"" + Convert.ToInt64(v).ToString(CultureInfo.InvariantCulture) + "\"}";
            if (v is float || v is double)
                return "{\"doubleValue\":" + Convert.ToDouble(v).ToString(CultureInfo.InvariantCulture) + "}";
            if (v is bool)
                return "{\"booleanValue\":" + ((bool)v ? "true" : "false") + "}";
            return "{\"stringValue\":\"" + MiniJson.Escape(v == null ? "" : v.ToString()) + "\"}";
        }

        /// <summary>Turns Firestore's typed "fields" object into name -> scalar string.</summary>
        static Dictionary<string, string> ExtractFields(object fieldsObj)
        {
            var result = new Dictionary<string, string>();
            var map = MiniJson.Obj(fieldsObj);
            if (map == null) return result;
            foreach (var kv in map)
                result[kv.Key] = ScalarOf(kv.Value);
            return result;
        }

        static string ScalarOf(object typedValue)
        {
            var m = MiniJson.Obj(typedValue);
            if (m == null) return null;
            if (m.ContainsKey("stringValue"))    return m["stringValue"]?.ToString();
            if (m.ContainsKey("integerValue"))   return m["integerValue"]?.ToString();
            if (m.ContainsKey("doubleValue"))    return m["doubleValue"]?.ToString();
            if (m.ContainsKey("booleanValue"))   return m["booleanValue"]?.ToString();
            if (m.ContainsKey("timestampValue")) return m["timestampValue"]?.ToString();
            return null;
        }

        // ---------- small parse helpers ----------

        public static int ToInt(Dictionary<string, string> f, string key, int fallback = 0)
        {
            string raw;
            if (f != null && f.TryGetValue(key, out raw) && !string.IsNullOrEmpty(raw))
            {
                // integerValue arrives as a plain integer string; doubleValue may have a decimal.
                double d;
                if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                    return (int)d;
            }
            return fallback;
        }

        public static string ToStr(Dictionary<string, string> f, string key, string fallback = "")
        {
            string raw;
            if (f != null && f.TryGetValue(key, out raw) && raw != null) return raw;
            return fallback;
        }
    }
}
