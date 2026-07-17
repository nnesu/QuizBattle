using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace QuizBattle.Services;

public class CloudinaryService
{
    private readonly Cloudinary cloudinary;

    public CloudinaryService()
    {
        Account account =
            new Account(
                "j3fal3hz",
                "451353949589417",
                "uAgvIC6gyYs8Bz62pxfE8gStrZU");

        cloudinary =
            new Cloudinary(
                account);
    }

    public async Task<string> UploadImage(
        FileResult fileResult)
    {
        Stream stream =
            await fileResult.OpenReadAsync();

        System.Diagnostics.Debug.WriteLine(
            $"Stream Length: {stream.Length}");

        FileDescription file =
            new FileDescription(
                fileResult.FileName,
                stream);

        ImageUploadParams parameters =
            new ImageUploadParams
            {
                File = file
            };

        ImageUploadResult result =
            await cloudinary.UploadAsync(
                parameters);

        System.Diagnostics.Debug.WriteLine(
            $"Status: {result.StatusCode}");

        System.Diagnostics.Debug.WriteLine(
            $"Error: {result.Error?.Message}");

        System.Diagnostics.Debug.WriteLine(
            $"URL: {result.SecureUrl}");

        if (result.Error != null)
        {
            throw new Exception(
                result.Error.Message);
        }

        if (result.SecureUrl == null)
        {
            throw new Exception(
                "Cloudinary returned no URL.");
        }

        return result.SecureUrl.ToString();
    }

    public async Task<string> UploadProfilePicture(
    string localId,
    FileResult fileResult)
    {
        Stream stream =
            await fileResult.OpenReadAsync();

        FileDescription file =
            new FileDescription(
                $"{localId}.jpg",
                stream);

        ImageUploadParams parameters =
            new ImageUploadParams
            {
                File = file,
                PublicId = $"profiles/{localId}",
                Overwrite = true
            };

        ImageUploadResult result =
            await cloudinary.UploadAsync(
                parameters);

        if (result.Error != null)
        {
            throw new Exception(
                result.Error.Message);
        }

        if (result.SecureUrl == null)
        {
            throw new Exception(
                "Cloudinary returned no URL.");
        }

        return result.SecureUrl.ToString();
    }
}