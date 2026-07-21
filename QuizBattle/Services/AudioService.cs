using Plugin.Maui.Audio;

namespace QuizBattle.Services;

public class AudioService
{
    private static AudioService? _instance;
    public static AudioService Instance => _instance ??= new AudioService(AudioManager.Current);

    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _bgmPlayer;

    public AudioService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    #region BGM Management

    public async Task PlayBgmAsync(string fileName = "bgm_lobby.mp3")
    {
        try
        {
            if (_bgmPlayer != null && _bgmPlayer.IsPlaying)
                return;

            StopBgm();

            var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            _bgmPlayer = _audioManager.CreatePlayer(stream);
            _bgmPlayer.Loop = true;
            _bgmPlayer.Volume = 0.4;
            _bgmPlayer.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing BGM: {ex.Message}");
        }
    }

    public void StopBgm()
    {
        try
        {
            if (_bgmPlayer != null)
            {
                if (_bgmPlayer.IsPlaying)
                    _bgmPlayer.Stop();

                _bgmPlayer.Dispose();
                _bgmPlayer = null;
            }
        }
        catch (ObjectDisposedException)
        {
            _bgmPlayer = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping BGM: {ex.Message}");
        }
    }

    /// <summary>
    /// Alias method to stop lobby background music.
    /// </summary>
    public static void StopLobbyMusic()
    {
        Instance.StopBgm();
    }

    #endregion

    #region SFX Management

    public async Task PlaySfxAsync(string fileName = "sfx_click.mp3")
    {
        try
        {
            var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            var sfxPlayer = _audioManager.CreatePlayer(stream);
            sfxPlayer.Volume = 1.0;
            sfxPlayer.Play();
            // Let Plugin.Maui.Audio natively handle audio release when playback ends
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing SFX: {ex.Message}");
        }
    }

    /// <summary>
    /// Static shortcut helper to play standard button click SFX.
    /// </summary>
    public static Task PlayButtonClickAsync()
    {
        return Instance.PlaySfxAsync("sfx_click.mp3");
    }

    #endregion
}   