using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Storage;

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
        GPIOControl gPIOControl;
        String lastFileName;
        FFT fft;

        public async void Start()
        {
            lastFileName = "test.mp3";
            await CreateGraph();
            await CreateAudioDeviceOutputNode();
            await CreateAudioDeviceInputNode();
            MicState(false);
            await CreateAudioFileInputNode("");
            CreateAudioFrameOutputNode();
            ConnectNodes();
            CreateGPIOControll();
            fft = new FFT();
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

        // GPIOControl erstellen und initialisieren
        private void CreateGPIOControll()
        {
            //gPIOControl = new GPIOControl();      //zum verwenden mit RaspberryPi
            gPIOControl = new GPIOControlVirtual(); //zum verwenden ohne RaspberryPi
            gPIOControl.Init();
        }


        unsafe private void ProcessFrameOutput(AudioFrame frame)
        {
            // holt ein AudioBuffer Objekt aus dem Frame
            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            //IMemoryBufferReference aus buffer holen
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                int count = 0;
                byte* dataInBytes;
                uint capacityInBytes;
                float* dataInFloat;
               // double[] dataInDouble = new Double[(int)audioGraph.EncodingProperties.SampleRate];
                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                dataInFloat = (float*)dataInBytes;
                Debug.WriteLine(buffer.Length);

                float lows = 0;
                float mids = 0;
                float hights = 0;
                for (uint i = 0; i < (buffer.Length / 4) / 3; i += 100)
                {
                    lows += dataInFloat[i];
                }
                for (uint i = (buffer.Length / 4) / 3; i < (buffer.Length / 4) * (2 / 3); i += 100)
                {
                    mids += dataInFloat[i];
                }
                for (uint i = (buffer.Length / 4) * (2 / 3); i < (buffer.Length / 4); i += 100)
                {
                    hights += dataInFloat[i];
                }

                if (lows > mids && lows > hights)
                {
                    gPIOControl.SetRed(false);
                    gPIOControl.SetYellow(false);
                    gPIOControl.SetGreen(true);

                }
                else
                {
                    if (mids > hights)
                    {
                        gPIOControl.SetRed(false);
                        gPIOControl.SetYellow(true);
                        gPIOControl.SetGreen(false);
                    }
                    else
                    {
                        gPIOControl.SetRed(true);
                        gPIOControl.SetYellow(false);
                        gPIOControl.SetGreen(false);
                    }
                }



                //dataInDouble[count] = (double)*dataInFloat;

                //count++;
                //if (count < (int)audioGraph.EncodingProperties.SampleRate)
                //{
                //    SetLight(dataInDouble);
                //    count = 0;
                //}
                //gPIOControll.SetGreen(dataInFloat[1] < 0.0);
            }
        }

        public void SetLight(double[] fft)
        {

            //double avg = 0;
            Debug.WriteLine(fft.Length);
            //for (int i = 0; i < fft.Length; i++)
            //{
            //    Debug.Write(fft[i] + " ");
            //    avg += fft[i];
            //}
            //Debug.WriteLine("");
            //avg /= fft.Length;
            //gPIOControl.SetGreen(avg < 200);
            //gPIOControl.SetYellow(avg > 200 && avg < 5000);
            //gPIOControl.SetRed(avg > 5000);
        }

        public void CreateAudioFrameOutputNode()
        {
            //int sampleRate = (int)audioGraph.EncodingProperties.SampleRate;
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
    }
}
