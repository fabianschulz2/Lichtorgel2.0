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
        AudioGraph audioGraph;
        AudioFileInputNode audioFileInputNode;
        AudioDeviceInputNode audioDeviceInputNode;
        AudioDeviceOutputNode audioDeviceOutputNode;
        AudioFrameOutputNode audioFrameOutputNode;
        GPIOControll gPIOControll;

        public async void Start(bool filePlay, bool micOn, String audioFileName)
        {
            await CreateGraph();
            await CreateDefaultDeviceOutputNode();
            FileState(filePlay, audioFileName);  // erstellt FileInputNode
            MicState(micOn);  // erstellt DeviceInputNode
            CreateGPIOControll();
            CreateFrameOutputNode();
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
        private async Task CreateDefaultDeviceOutputNode()
        {
            CreateAudioDeviceOutputNodeResult result = await audioGraph.CreateDeviceOutputNodeAsync();

            if (result.Status != AudioDeviceNodeCreationStatus.Success)
            {
                throw new Exception(result.Status.ToString());
            }

            audioDeviceOutputNode = result.DeviceOutputNode;
        }

        // Input vom default Mikrofon
        private async Task CreateDefaultDeviceInputNode()
        {
            CreateAudioDeviceInputNodeResult result = await audioGraph.CreateDeviceInputNodeAsync(MediaCategory.Media);

            if (result.Status != AudioDeviceNodeCreationStatus.Success)
            {
                throw new Exception(result.Status.ToString());
            }

            audioDeviceInputNode = result.DeviceInputNode;
        }

        // Input von ausgewaehlter Datei
        private async Task CreateFileInputNode(String fileName)
        {
            StorageFile file = null;

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
            StorageFolder Folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            Folder = await Folder.GetFolderAsync("Assets");
            file = await Folder.GetFileAsync(fileName);

            CreateAudioFileInputNodeResult result = await audioGraph.CreateFileInputNodeAsync(file);

            if (result.Status != AudioFileNodeCreationStatus.Success)
            {
                throw new Exception(result.Status.ToString());
            }


            audioFileInputNode = result.FileInputNode;
        }

        // Nodes Zusammenfuegen
        private void ConnectNodes()
        {
            audioFileInputNode.AddOutgoingConnection(audioDeviceOutputNode);
            audioDeviceInputNode.AddOutgoingConnection(audioDeviceOutputNode);

            audioFileInputNode.AddOutgoingConnection(audioFrameOutputNode);
            audioDeviceInputNode.AddOutgoingConnection(audioFrameOutputNode);
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
                byte* dataInBytes;
                uint capacityInBytes;
                float* dataInFloat;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                dataInFloat = (float*)dataInBytes;

                gPIOControll.SetGreen(dataInFloat[1] < 0.0);


            }
        }
        public void CreateFrameOutputNode()
        {
            audioFrameOutputNode = audioGraph.CreateFrameOutputNode();
            audioGraph.QuantumStarted += AudioGraph_QuantumStarted;
        }

        private void AudioGraph_QuantumStarted(AudioGraph sender, object args)
        {
            AudioFrame frame = audioFrameOutputNode.GetFrame();
            ProcessFrameOutput(frame);
        }
        public async void MicState(bool micOn)
        {
            if (micOn)
            {
                await CreateDefaultDeviceInputNode();
            }
            else
            {
                if (audioDeviceInputNode != null)
                {
                    audioDeviceInputNode.Stop();
                }
            }
        }
        public async void FileState(bool filePlay, String fileName)
        {
            if (filePlay)
            {
                if (fileName == null)
                {
                    MessageDialog dialog = new MessageDialog("Bitte wähle eine Audiodatei aus");
                    await dialog.ShowAsync();
                }
                else
                {
                    await CreateFileInputNode(fileName);
                }
            }
            else
            {
                if (audioFileInputNode != null)
                {
                    audioFileInputNode.Stop();
                }
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
