using UnityEngine;

namespace KOK.Audio
{
    public class RecordingMicrophone
    {
        public string Name { get; set; }
        public int MinimumFrequency { get; set; }
        public int MaximumFrequency { get; set; }
    }

    [RequireComponent(typeof(AudioSource))]
    public class VoiceRecorder : MonoBehaviour
    {
        const int MAX_RECORDING_DURATION_SECONDS = 600; // 10 minutes
        const int DEFAULT_SAMPLE_RATES = 48000; // 48khz

        public string FileName
        {
            get => fileName;
            set
            {
                fileName = value;
            }
        }

        private static AudioSource audioSource;
        private bool isRecording = false;
        [SerializeField] private string fileName = string.Empty;
        private RecordingMicrophone currentMicrophone;
        private float startRecordTime;
        private float recordCountdown;
        private string recordingSaveLocation;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            isRecording = false;
            recordingSaveLocation = Application.persistentDataPath + "/Recordings/";
            InitializeMicrophone();
        }

        private void Update()
        {
            #region Testing
            if (Input.GetKeyDown(KeyCode.Q))
            {
                StartRecording();
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                StopRecording();
            }
            #endregion

            AutomaticallyStopRecording();
        }

        /// <summary>
        /// Automatically stop and save the voice recording when the audio clip reach maximum duration.
        /// </summary>
        private void AutomaticallyStopRecording()
        {
            // Check if only is recording player voice
            if (isRecording)
            {
                recordCountdown -= Time.deltaTime;
                if (recordCountdown <= 0f)
                {
                    Debug.Log("Recording has exceeds maximum length (10 minutes). Automatically stop and save recording!");
                    StopRecording();
                }
            }
        }

        private void InitializeMicrophone()
        {
            // Detecting microphone
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("No microphone detected!");
                currentMicrophone = null;
                return;
            }

            Microphone.GetDeviceCaps(Microphone.devices[0], out int minFrequency, out int maxFrequency);
            // Select the first microphone found in the device
            currentMicrophone = new RecordingMicrophone()
            {
                Name = Microphone.devices[0],
                MinimumFrequency = minFrequency,
                MaximumFrequency = maxFrequency
            };

            Debug.Log($"Using microphone [{currentMicrophone.Name}] with frequency: [{currentMicrophone.MinimumFrequency}] - [{currentMicrophone.MaximumFrequency}]");
        }

        [ContextMenu("List Available Microphones")]
        public void ListMicrophoneDevices()
        {
            Debug.Log("Listing all connected microphone devices:");
            for (int i = 0; i < Microphone.devices.Length; i++)
            {
                Microphone.GetDeviceCaps(Microphone.devices[i], out int minFrequency, out int maxFrequency);
                Debug.Log($"#{i}: {Microphone.devices[i]} -- Min: {minFrequency} - Max: {maxFrequency}");
            }
        }

        public void StartRecording()
        {
            // Check if is already in recording session
            if (isRecording)
            {
                return;
            }
            // Check current microphone
            if (currentMicrophone == null)
            {
                Debug.LogError("Failed to start recording. No microphone detected!");
                return;
            }
            isRecording = true;
            startRecordTime = Time.time;
            recordCountdown = MAX_RECORDING_DURATION_SECONDS;
            audioSource.clip = Microphone.Start(currentMicrophone.Name, false, MAX_RECORDING_DURATION_SECONDS, currentMicrophone.MaximumFrequency);
            Debug.Log("Start recording!");
        }

        public void StopRecording()
        {
            if (!isRecording)
            {
                return;
            }
            isRecording = false;
            SaveAudioClipToMp3();
            Microphone.End(currentMicrophone.Name);
            Debug.Log("Stop recording!");
        }

        private void SaveAudioClipToMp3()
        {
            AudioClip trimmedClip = ExtractRecordedSound(audioSource.clip, currentMicrophone.MaximumFrequency);
            string filePath = recordingSaveLocation + fileName + ".wav";
            WavHelper.SaveWavFile(filePath, trimmedClip);
        }

        /// <summary>
        /// Extracts a portion of the original audio clip based on the elapsed recording time.
        /// </summary>
        /// <param name="originalClip">The original audio clip to extract from.</param>
        /// <returns>A new AudioClip containing the recorded sound.</returns>
        private AudioClip ExtractRecordedSound(AudioClip originalClip, int sampleRates)
        {
            // Check if the input clip is null
            if (originalClip == null)
            {
                Debug.LogError("Input clip is null. Cannot extract recorded sound.");
                return null;
            }
            // Calculate the time elapsed since recording started
            float timeSinceRecordStarted = Time.time - startRecordTime;

            // Calculate the samples per second (sampling rate)
            float samplesPerSec = originalClip.samples / originalClip.length;

            // Determine the number of recorded samples based on elapsed time
            int recordedSampleCount = Mathf.FloorToInt(samplesPerSec * timeSinceRecordStarted);

            // Create an array to store the recorded audio samples
            float[] recordedSamples = new float[recordedSampleCount];
            originalClip.GetData(recordedSamples, 0);

            // Create a new AudioClip to hold the recorded sound
            AudioClip recordedClip = AudioClip.Create("RecordedSound", recordedSampleCount, 1, sampleRates, false);
            recordedClip.SetData(recordedSamples, 0);

            // Return the newly created recorded clip
            return recordedClip;
        }
    }
}
