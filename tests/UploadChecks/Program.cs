using Microsoft.Extensions.Primitives;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static FormFile File(byte[] bytes, string name = "fake.txt") =>
    new(new MemoryStream(bytes), 0, bytes.Length, "image", name);

Check(await ImageUpload.ValidateAsync(File([137,80,78,71,13,10,26,10,0,0,0,0]), default) == "png", "PNG signature");
Check(await ImageUpload.ValidateAsync(File([255,216,255,0,0,0,0,0,0,0,0,0]), default) == "jpg", "JPEG signature");
Check(await ImageUpload.ValidateAsync(File("RIFF1234WEBP"u8.ToArray()), default) == "webp", "WebP signature");
Check(await ImageUpload.ValidateAsync(File("<script>bad</script>"u8.ToArray(), "tire.png"), default) is null, "Reject disguised non-image");
Check(await ImageUpload.ValidateAsync(File([]), default) is null, "Reject empty file");
Check(await ImageUpload.ValidateAsync(File(new byte[ImageUpload.MaxBytes + 1]), default) is null, "Reject oversized file");

var fields = new Dictionary<string, StringValues>
{
    ["brand"] = "Michelin", ["type"] = "Winter", ["rimDiameter"] = "17", ["price"] = "129.99"
};
var errors = new Dictionary<string, string[]>();
var tire = TireForm.Read(new FormCollection(fields), errors);
Check(errors.Count == 0 && tire.Price == 129.99m && tire.Type == TireType.Winter, "Parse valid multipart fields");
fields["brand"] = new string(' ', 60) + "Michelin" + new string(' ', 60);
errors.Clear();
tire = TireForm.Read(new FormCollection(fields), errors);
Check(errors.Count == 0 && tire.Brand == "Michelin", "Normalize padded brands before validation and persistence");
fields["brand"] = new string('a', 51);
errors.Clear();
TireForm.Read(new FormCollection(fields), errors);
Check(errors.ContainsKey("Brand"), "Reject brands exceeding the database length limit");
fields["rimDiameter"] = "17.5";
fields["price"] = "1,000";
fields["type"] = "999";
fields["brand"] = "a";
errors.Clear();
TireForm.Read(new FormCollection(fields), errors);
Check(errors.Keys.Order().SequenceEqual(new[] { "Brand", "Price", "RimDiameter", "Type" }.Order()), "Validate four field types");
Console.WriteLine("All upload and form validation checks passed.");
