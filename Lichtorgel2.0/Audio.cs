using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media;
using Windows.UI.Xaml;
using Windows.Media.Render;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Accord.Math;
using Windows.UI.Popups;

namespace Lichtorgel2._0
{

    //siehe: https://docs.microsoft.com/de-de/windows/uwp/audio-video-camera/audio-graphs

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    class Audio
    {
        AudioGraph audioGraph = null;
        AudioFileInputNode audioFileInputNode = null;
        AudioDeviceInputNode audioDeviceInputNode = null;
        AudioDeviceOutputNode audioDeviceOutputNode = null;
        AudioFrameOutputNode audioFrameOutputNode = null;
        GPIOControll gPIOControll;
        String lastFileName;

        public async void Start(bool filePlay, bool micOn, String fileName)
        {
            lastFileName = "test.mp3";
            await CreateGraph();
            await CreateAudioDeviceOutputNode();
            await CreateAudioDeviceInputNode();
            await CreateAudioFileInputNode(fileName);
            CreateAudioFrameOutputNode();
            CreateGPIOControll();

            ConnectNodes();
            audioGraph.Start();
        }


        // AudioGraph generieren   
        private async Task CreateGraph()
        {
            // Specify settings for graph, the AudioRenderCategory helps to optimize audio processing
            AudioGraphSettings settings = new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Media);

            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                throw new Exception(result.Status.ToString());
            }

            audioGraph = result.Graph;
        }

        // output zum default Ausgabegerät
        private async Task CreateAudioDeviceOutputNode()
        {
            CreateAudioDeviceOutputNodeResult result = await audioGraph.CreateDeviceOutputNodeAsync();

            if (result.Status != AudioDeviceNodeCreationStatus.Success)
            {
                throw new Exception(result.Status.ToString());
            }

            audioDeviceOutputNode = result.DeviceOutputNode;
        }

        // Input vom default Mikrofon
        private async Task CreateAudioDeviceInputNode()
        {
            CreateAudioDeviceInputNodeResult result = await audioGraph.CreateDeviceInputNodeAsync(MediaCategory.Media);

            if (result.Status != AudioDeviceNodeCreationStatus.Success)
            {
                throw new Exception(result.Status.ToString());
            }

            audioDeviceInputNode = result.DeviceInputNode;
        }

        // Input von ausgewaehlter Datei
        private async Task CreateAudioFileInputNode(String fileName)
        {


            /*   if (song.Equals(""))
               {
                   FileOpenPicker filePicker = new FileOpenPicker
                   {
                       SuggestedStartLocation = PickerLocationId.MusicLibrary,
                       FileTypeFilter = { ".mp3", ".wav" }
                   };
                   file = await filePicker.PickSingleFileAsync();

               } 
               */


            //Standartdatei auswählen (bei Windows IoT)
            StorageFile file;
            StorageFolder Folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            Folder = await Folder.GetFolderAsync("Assets");
            if (fileName.Equals("")) //bei der Initialisierung
            {
                file = await Folder.GetFileAsync(lastFileName);
            }
            else
            {
                file = await Folder.GetFileAsync(fileName);
            }


            CreateAudioFileInputNodeResult result = await audioGraph.CreateFileInputNodeAsync(file);

            if (result.Status != AudioFileNodeCreationStatus.Success)
            {
                throw new Exception(result.Status.ToString());
            }


            audioFileInputNode = result.FileInputNode;
            if (fileName.Equals("")) //bei der Initialisierung
            {
                audioFileInputNode.ConsumeInput = false;
            }
        }

        // Nodes Zusammenfuegen
        private void ConnectNodes()
        {
            if (audioFileInputNode != null)
            {
                audioFileInputNode.AddOutgoingConnection(audioDeviceOutputNode);
                audioFileInputNode.AddOutgoingConnection(audioFrameOutputNode);
            }
            if (audioDeviceInputNode != null)
            {
                audioDeviceInputNode.AddOutgoingConnection(audioDeviceOutputNode);
                audioDeviceInputNode.AddOutgoingConnection(audioFrameOutputNode);
            }
        }

        // GPIOControll erstellen und initialisieren
        private void CreateGPIOControll()
        {
            gPIOControll = new GPIOControll();
            gPIOControll.Init();
        }


        unsafe private void ProcessFrameOutput(AudioFrame frame)
        {
            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                  int count = 0;
                byte* dataInBytes;
                uint capacityInBytes;
                float* dataInFloat;
                 double[] dataInDouble = new Double[(int)audioGraph.EncodingProperties.SampleRate];
                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                dataInFloat = (float*)dataInBytes;
                 dataInDouble[count] = (double)*dataInFloat;
                count++;
                    if (count < (int)audioGraph.EncodingProperties.SampleRate)
                {
                           SetLight(FFT(dataInDouble));
                           count = 0;
                }

                 //   gPIOControll.SetGreen(dataInFloat[1] < 0.0);


            }
        }

        public void SetLight(double[] fft)
        {
            double avg = 0;
            for (int i = 0; i < fft.Length; i++)
            {
                avg += fft[i];
            }
            avg /= fft.Length;
            gPIOControll.SetGreen(avg < 200);
            gPIOControll.SetYellow(avg > 200 && avg < 5000);
            gPIOControll.SetRed(avg > 5000);

        }
        public void CreateAudioFrameOutputNode()
        {
            int sampleRate = (int)audioGraph.EncodingProperties.SampleRate;
            audioFrameOutputNode = audioGraph.CreateFrameOutputNode();
            audioGraph.QuantumStarted += AudioGraph_QuantumStarted;
        }

        private void AudioGraph_QuantumStarted(AudioGraph sender, object args)
        {
            AudioFrame frame = audioFrameOutputNode.GetFrame();
            ProcessFrameOutput(frame);
        }
        public void MicState(bool micOn)
        {
            //unmute
            if (micOn)
            {
                audioDeviceInputNode.ConsumeInput = true; 
            }
            //Mikrofon mute
            else
            {
                audioDeviceInputNode.ConsumeInput = false; 
            }
        }

        public async void FileState(bool filePlay, String fileName)
        {
            //wiedergabe fortsetzen, wenn sich der Dateiname aendert, wird ein neuer FileInputNode erstellt.
            if (filePlay)
            {
                if (!fileName.Equals(lastFileName))
                {
                    audioFileInputNode.RemoveOutgoingConnection(audioDeviceOutputNode);
                    audioFileInputNode.RemoveOutgoingConnection(audioFrameOutputNode);
                    await CreateAudioFileInputNode(fileName);
                    audioFileInputNode.AddOutgoingConnection(audioDeviceOutputNode);
                    audioFileInputNode.AddOutgoingConnection(audioFrameOutputNode);
                    lastFileName = fileName; 
                }

                audioFileInputNode.ConsumeInput = true;

            }
            //wiedergabe pausieren
            else
            {
                audioFileInputNode.ConsumeInput = false;
            }
        }

        public double[] FFT(double[] data)

        {

            double[] fft = new double[data.Length];

            System.Numerics.Complex[] fftComplex = new System.Numerics.Complex[data.Length];

            for (int i = 0; i < data.Length; i++)

                fftComplex[i] = new System.Numerics.Complex(data[i], 0.0);

            FourierTransform.FFT(fftComplex, FourierTransform.Direction.Forward);

            for (int i = 0; i < data.Length; i++)

                fft[i] = fftComplex[i].Magnitude;

            return fft;

        }


    }
}
