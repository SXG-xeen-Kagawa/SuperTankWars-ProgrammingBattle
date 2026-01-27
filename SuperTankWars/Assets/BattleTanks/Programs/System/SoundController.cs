using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SXG2025.Effect {
    public class SoundController : MonoBehaviour
    {
        private static SoundController instance = null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnGameStart()
        {
            if (instance == null)
            {
                // プレハブをロードしてインスタンスとして保持
                GameObject prefab = Resources.Load<GameObject>("SoundManager");
                instance = Instantiate(prefab).GetComponent<SoundController>();

                // シーン遷移時に破棄されないように設定
                DontDestroyOnLoad(instance.gameObject);
            }
        }

        [System.Serializable]
        public enum SEType
        {
            Dondon,
            TimeUp,
            CountDown,
            Start,
            RollEnd,
            Waaa,
            Count,
            GameStart,
            RankSet,
            TrophyShines,
            Cheers,
            Rematch,
            Shot,
            Hit,
            Dead,
            ResultGauge,
        }

        [System.Serializable]
        public enum BGMType
        {
            Title,
            MainBGM,
            Roll,
            Setting,
            Replay,
            Tournament,
            TournamentResult,
            Null,
        }

        [System.Serializable]
        public struct SEData {
            public SEType type;
            public AudioClip clip;
            public float minPitch;
            public float maxPitch;
        }


        [SerializeField]
        private AudioSource[] _audioSources;

        [SerializeField]
        private AudioSource _damageAudioSource;

        [SerializeField]
        private AudioSource _countAudioSource;

        [SerializeField]
        private List<SEData> _datas;

        [SerializeField]
        private AudioSource _bgmSource;

        [SerializeField]
        private AudioClip[] _bgms;

        private float _baseBGMvol = 0.55f;
        private int _audioCount = 0;
        private BGMType _nowBGM = BGMType.Null;

        static public void SetBGMVol(float vol)
        {
            instance?._SetBGMVol(vol);
        }

        static public void PlaySE(SEType type, float pan = 0.0f)
        {
            instance?._PlaySE(type, pan);
        }

        static public void PlayBGM(BGMType type)
        {
            instance?._PlayBGM(type);
        }

        static public void StopBGM()
        {
            instance?._StopBGM();
        }

        static public void FadeOutBGM()
        {
            instance?._FadeOutBGM();
        }

        static public void FadeInBGM(BGMType type)
        {
            instance?._FadeInBGM(type);
        }

        private void _SetBGMVol(float vol)
        {
            _baseBGMvol = 0.55f * vol;
        }

        private void _PlaySE(SEType type, float pan = 0.0f)
        {
#if ON_EFFECT_XEEN
            var clips = _datas.FindAll(_ => _.type == type);
            if (clips.Count == 0) return;
            var data = clips[Random.Range(0, clips.Count)];
            if (data.clip == null) return;

            if (type == SEType.Count)
            {
                _countAudioSource.Stop();
                _countAudioSource.PlayOneShot(data.clip);
            }
            else
            {
                var id = _audioCount;
                _audioCount = (_audioCount + 1) % _audioSources.Length;

                if (type == SEType.Hit || type == SEType.Shot)
                {
                    _audioSources[id].volume = 0.3f;
                }
                else
                {
                    _audioSources[id].volume = 0.7f;
                }
                _audioSources[id].Stop();
                _audioSources[id].pitch = Random.Range(data.minPitch, data.maxPitch);
                _audioSources[id].panStereo = pan;
                _audioSources[id].PlayOneShot(data.clip);
            }
#endif
        }

        private void _PlayBGM(BGMType type)
        {
            PlayBGM(type, _baseBGMvol);
        }

        private void _StopBGM()
        {
            StopAllCoroutines();
#if ON_EFFECT_XEEN
            _nowBGM =  BGMType.Null;
            _bgmSource.Stop();
#endif
        }

        private void _FadeOutBGM()
        {
            StopAllCoroutines();
#if ON_EFFECT_XEEN
            _nowBGM = BGMType.Null;
            StartCoroutine(FadeOut());
#endif
            SetBGMVol(1.0f);
        }

        private void _FadeInBGM(BGMType type)
        {
            StopAllCoroutines();
#if ON_EFFECT_XEEN
            PlayBGM(type, 0);
            StartCoroutine(FadeIn());
#endif
        }

        // ------

        private void PlayBGM(BGMType type, float volume)
        {
            StopAllCoroutines();
#if ON_EFFECT_XEEN
            if (_bgms.Length < (int)type
                || (_nowBGM != BGMType.Null && _nowBGM == type))
            {
                // BGMが設定されていない
                // または、連続して同じBGMを指定している
                return;
            }
            if (_bgms[(int)type] == null) return;
            _bgmSource.Stop();
            _nowBGM = type;
            _bgmSource.volume = _baseBGMvol;
            _bgmSource.clip = _bgms[(int)type];
            if (type == BGMType.Roll)
            {
                _bgmSource.loop = false;
            }
            else
            {
                _bgmSource.loop = true;
            }
            _bgmSource.Play();
#endif
        }

        private IEnumerator FadeOut()
        {
            float time = 0f;
            float FadeTime = 1f;
            while (time / FadeTime < 1f)
            {
                _bgmSource.volume = Mathf.Lerp(_baseBGMvol, 0f, time / FadeTime);
                yield return new WaitForEndOfFrame();
                time += Time.deltaTime;
            }
            _bgmSource.volume = 0f;
            _bgmSource.Stop();
        }

        private IEnumerator FadeIn()
        {
            float time = 0f;
            float FadeTime = 2f;
            while (time / FadeTime < 1f)
            {
                _bgmSource.volume = Mathf.Lerp(0f, _baseBGMvol, time / FadeTime);
                yield return new WaitForEndOfFrame();
                time += Time.deltaTime;
            }
            _bgmSource.volume = _baseBGMvol;
        }
    }
}