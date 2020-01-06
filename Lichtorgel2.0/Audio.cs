using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Media.Audio;
using Windows.Media.Capture;

namespace Lichtorgel2._0
{
    class Audio
    {
        AudioFileInputNode _fileInputNode;
        AudioDeviceInputNode _deviceInputNode;
        AudioGraph _graph;
        AudioDeviceOutputNode _deviceOutputNode;

        public async void Start()
        {
            await CreateGraph();
            await CreateDefaultDeviceOutputNode();
            await CreateDefaultDeviceInputNode();
            await CreateFileInputNode();

            AddReverb();

            ConnectNodes();

            _graph.Start();
        }
        public AudioDeviceOutputNode GetOutputNode()
        {
            return _deviceOutputNode;
        }

        /// <summary>
        /// Create an audio graph that can contain nodes
        /// </summary>       
        private async Task CreateGraph()
        {
            // Specify settings for graph, the AudioRenderCategory helps to optimize audio processing
            AudioGraphSettings settings = new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Media);

            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                throw new Exception(result.Status.ToString());
            }

            _graph = result.Graph;
        }

        /// <summary>
        /// Create a node to output audio data to the default audio device (e.g. soundcard)
        /// </summary>
        private async Task CreateDefaultDeviceOutputNode()
        {
            CreateAudioDeviceOutputNodeResult result = await _graph.CreateDeviceOutputNodeAsync();

            if (result.Status != AudioDeviceNodeCreationStatus.Success)
            {
                throw new Exception(result.Status.ToString());
            }

            _deviceOutputNode = result.DeviceOutputNode;
        }

        // Mikrofon hinzufuegen
        private async Task CreateDefaultDeviceInputNode()
        {
            CreateAudioDeviceInputNodeResult result = await _graph.CreateDeviceInputNodeAsync(MediaCategory.Media);

            if (result.Status != AudioDeviceNodeCreationStatus.Success)
            {
                throw new Exception(result.Status.ToString());
            }

            _deviceInputNode = result.DeviceInputNode;
        }

        /// <summary>
        /// Ask user to pick a file and use the chosen file to create an AudioFileInputNode
        /// </summary>
        private async Task CreateFileInputNode()
        {
            FileOpenPicker filePicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                FileTypeFilter = { ".mp3", ".wav" }
            };

            StorageFile file = await filePicker.PickSingleFileAsync();

            if (file != null)
            {
                // Application now has read/write access to the picked file

            }
            else
            {
                StorageFolder Folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                Folder = await Folder.GetFolderAsync("Assets");
                file = await Folder.GetFileAsync("test.mp3");
            }

            // file null check code omitted

            CreateAudioFileInputNodeResult result = await _graph.CreateFileInputNodeAsync(file);

            if (result.Status != AudioFileNodeCreationStatus.Success)
            {
                throw new Exception(result.Status.ToString());
            }


            _fileInputNode = result.FileInputNode;
        }

        /// <summary>
        /// Create an instance of the pre-supplied reverb effect and add it to the output node
        /// </summary>
        private void AddReverb()
        {
            ReverbEffectDefinition reverbEffect = new ReverbEffectDefinition(_graph)
            {
                DecayTime = 1
            };

            _deviceOutputNode.EffectDefinitions.Add(reverbEffect);
        }

        /// <summary>
        /// Connect all the nodes together to form the graph, in this case we only have 2 nodes
        /// </summary>
        private void ConnectNodes()
        {
            _fileInputNode.AddOutgoingConnection(_deviceOutputNode);
            _deviceInputNode.AddOutgoingConnection(_deviceOutputNode);

        }


    }
}
