using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace OBSWebsocketDotNet
{
    /// <summary>
    /// Instance of a connection with an obs-websocket server
    /// </summary>
    public partial class OBSWebsocket
    {
        #region Private Constants

        private const string REQUEST_FIELD_VOLUME_DB = "inputVolumeDb";
        private const string REQUEST_FIELD_VOLUME_MUL = "inputVolumeMul";
        private const string RESPONSE_FIELD_IMAGE_DATA = "imageData";

        #endregion

        /// <summary>
        /// Get basic OBS video information
        /// </summary>
        public ObsVideoSettings GetVideoSettings()
        {
            JsonElement response = SendRequest(nameof(GetVideoSettings));
            return JsonHelper.Deserialize<ObsVideoSettings>(response);
        }

        /// <summary>
        /// Saves a screenshot of a source to the filesystem.
        /// The `imageWidth` and `imageHeight` parameters are treated as \"scale to inner\", meaning the smallest ratio will be used and the aspect ratio of the original resolution is kept.
        /// If `imageWidth` and `imageHeight` are not specified, the compressed image will use the full resolution of the source.
        /// **Compatible with inputs and scenes.**
        /// </summary>
        /// <param name="sourceName">Name of the source to take a screenshot of</param>
        /// <param name="imageFormat">Image compression format to use. Use `GetVersion` to get compatible image formats</param>
        /// <param name="imageFilePath">Path to save the screenshot file to. Eg. `C:\\Users\\user\\Desktop\\screenshot.png`</param>
        /// <param name="imageWidth">Width to scale the screenshot to</param>
        /// <param name="imageHeight">Height to scale the screenshot to</param>
        /// <param name="imageCompressionQuality">Compression quality to use. 0 for high compression, 100 for uncompressed. -1 to use \"default\" (whatever that means, idk)</param>
        /// <returns>Base64-encoded screenshot string</returns>
        public string SaveSourceScreenshot(string sourceName, string imageFormat, string imageFilePath, int imageWidth = -1, int imageHeight = -1, int imageCompressionQuality = -1)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sourceName), sourceName },
                { nameof(imageFormat), imageFormat },
                { nameof(imageFilePath), imageFilePath }
            };

            if (imageWidth > -1)
            {
                requestData.Add(nameof(imageWidth), imageWidth);
            }
            if (imageHeight > -1)
            {
                requestData.Add(nameof(imageHeight), imageHeight);
            }
            if (imageCompressionQuality > -1)
            {
                requestData.Add(nameof(imageCompressionQuality), imageCompressionQuality);
            }

            var response = SendRequest(nameof(SaveSourceScreenshot), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.GetStringValue(response, "imageData");
        }

        /// <summary>
        /// Saves a screenshot of a source to the filesystem.
        /// The `imageWidth` and `imageHeight` parameters are treated as \"scale to inner\", meaning the smallest ratio will be used and the aspect ratio of the original resolution is kept.
        /// If `imageWidth` and `imageHeight` are not specified, the compressed image will use the full resolution of the source.
        /// **Compatible with inputs and scenes.**
        /// </summary>
        /// <param name="sourceName">Name of the source to take a screenshot of</param>
        /// <param name="imageFormat">Image compression format to use. Use `GetVersion` to get compatible image formats</param>
        /// <param name="imageFilePath">Path to save the screenshot file to. Eg. `C:\\Users\\user\\Desktop\\screenshot.png`</param>
        /// <returns>Base64-encoded screenshot string</returns>
        public string SaveSourceScreenshot(string sourceName, string imageFormat, string imageFilePath)
        {
            return SaveSourceScreenshot(sourceName, imageFormat, imageFilePath, -1, -1);
        }

        /// <summary>
        /// Executes hotkey routine, identified by hotkey unique name
        /// </summary>
        /// <param name="hotkeyName">Unique name of the hotkey, as defined when registering the hotkey (e.g. "ReplayBuffer.Save")</param>
        public void TriggerHotkeyByName(string hotkeyName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(hotkeyName), hotkeyName }
            };

            SendRequest(nameof(TriggerHotkeyByName), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Triggers a hotkey using a sequence of keys.
        /// </summary>
        /// <param name="keyId">Main key identifier (e.g. OBS_KEY_A for key "A"). Available identifiers are here: https://github.com/obsproject/obs-studio/blob/master/libobs/obs-hotkeys.h</param>
        /// <param name="keyModifier">Optional key modifiers object. You can combine multiple key operators. e.g. KeyModifier.Shift | KeyModifier.Control</param>
        public void TriggerHotkeyByKeySequence(OBSHotkey keyId, KeyModifier keyModifier = KeyModifier.None)
        {
            var keyModifiersData = new Dictionary<string, object>
            {
                { "shift", (keyModifier & KeyModifier.Shift) == KeyModifier.Shift },
                { "alt", (keyModifier & KeyModifier.Alt) == KeyModifier.Alt },
                { "control", (keyModifier & KeyModifier.Control) == KeyModifier.Control },
                { "command", (keyModifier & KeyModifier.Command) == KeyModifier.Command }
            };

            var requestData = new Dictionary<string, object>
            {
                { nameof(keyId), keyId.ToString() },
                { "keyModifiers", keyModifiersData }
            };

            SendRequest(nameof(TriggerHotkeyByKeySequence), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Get the name of the currently active scene. 
        /// </summary>
        /// <returns>Name of the current scene</returns>
        public string GetCurrentProgramScene()
        {
            JsonElement response = SendRequest(nameof(GetCurrentProgramScene));
            return JsonHelper.GetStringValue(response, "currentProgramSceneName");
        }

        /// <summary>
        /// Set the current scene to the specified one
        /// </summary>
        /// <param name="sceneName">The desired scene name</param>
        public void SetCurrentProgramScene(string sceneName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName }
            };

            SendRequest(nameof(SetCurrentProgramScene), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Get OBS stats (almost the same info as provided in OBS' stats window)
        /// </summary>
        public ObsStats GetStats()
        {
            JsonElement response = SendRequest(nameof(GetStats));
            return JsonHelper.Deserialize<ObsStats>(response);
        }

        /// <summary>
        /// List every available scene
        /// </summary>
        /// <returns>A <see cref="List{SceneBasicInfo}" /> of <see cref="SceneBasicInfo"/> objects describing each scene</returns>
        public List<SceneBasicInfo> ListScenes()
        {
            var response = GetSceneList();
            return response.Scenes;
        }

        /// <summary>
        /// Get a list of scenes in the currently active profile
        /// </summary>
        public GetSceneListInfo GetSceneList()
        {
            JsonElement response = SendRequest(nameof(GetSceneList));
            return JsonHelper.Deserialize<GetSceneListInfo>(response);
        }

        /// <summary>
        /// Get the specified scene's transition override info
        /// </summary>
        /// <param name="sceneName">Name of the scene to return the override info</param>
        /// <returns>TransitionOverrideInfo</returns>
        public TransitionOverrideInfo GetSceneSceneTransitionOverride(string sceneName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName }
            };

            JsonElement response = SendRequest(nameof(GetSceneSceneTransitionOverride), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.Deserialize<TransitionOverrideInfo>(response);
        }

        /// <summary>
        /// Set specific transition override for a scene
        /// </summary>
        /// <param name="sceneName">Name of the scene to set the transition override</param>
        /// <param name="transitionName">Name of the transition to use</param>
        /// <param name="transitionDuration">Duration in milliseconds of the transition if transition is not fixed. Defaults to the current duration specified in the UI if there is no current override and this value is not given</param>
        public void SetSceneSceneTransitionOverride(string sceneName, string transitionName, int transitionDuration = -1)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(transitionName), transitionName }
            };

            if (transitionDuration >= 0)
            {
                requestData.Add(nameof(transitionDuration), transitionDuration);
            }

            SendRequest(nameof(SetSceneSceneTransitionOverride), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Remove any transition override from a scene
        /// </summary>
        /// <param name="sceneName">Name of the scene to remove the transition override</param>
        public void RemoveSceneSceneTransitionOverride(string sceneName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName }
            };

            SendRequest(nameof(RemoveSceneSceneTransitionOverride), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Set the T-Bar position (transition position between preview and program scenes)
        /// </summary>
        /// <param name="position">T-Bar position. This value must be between 0.0 and 1.0.</param>
        /// <param name="release">Whether or not the T-Bar gets released automatically after setting its new position</param>
        public void SetTBarPosition(double position, bool release = true)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(position), position },
                { nameof(release), release }
            };

            SendRequest(nameof(SetTBarPosition), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Toggles the status of the stream output.
        /// </summary>
        /// <returns>New state of the stream output</returns>
        public bool ToggleStream()
        {
            JsonElement response = SendRequest(nameof(ToggleStream));
            return JsonHelper.GetPropertyValue<bool>(response, "outputActive", false);
        }

        /// <summary>
        /// Start streaming. Will trigger an error if streaming is already active
        /// </summary>
        public void StartStream()
        {
            SendRequest(nameof(StartStream));
        }

        /// <summary>
        /// Stop streaming. Will trigger an error if streaming is not active.
        /// </summary>
        public void StopStream()
        {
            SendRequest(nameof(StopStream));
        }

        /// <summary>
        /// Toggles the status of the record output.
        /// </summary>
        public void ToggleRecord()
        {
            SendRequest(nameof(ToggleRecord));
        }

        /// <summary>
        /// Start recording. Will trigger an error if recording is already active
        /// </summary>
        public void StartRecord()
        {
            SendRequest(nameof(StartRecord));
        }        /// <summary>
        /// Stop recording. Will trigger an error if recording is not active.
        /// </summary>
        public string StopRecord()
        {
            JsonElement response = SendRequest(nameof(StopRecord));
            return JsonHelper.GetPropertyValue<string>(response, "outputPath", "");
        }

        /// <summary>
        /// Pause recording. Will trigger an error if recording is not active or is already paused.
        /// </summary>
        public void PauseRecord()
        {
            SendRequest(nameof(PauseRecord));
        }

        /// <summary>
        /// Resume recording. Will trigger an error if recording is not paused.
        /// </summary>
        public void ResumeRecord()
        {
            SendRequest(nameof(ResumeRecord));
        }

        /// <summary>
        /// Gets the status of the stream output
        /// </summary>
        /// <returns>An OutputStatus object describing the current outputs states</returns>
        public OutputStatus GetStreamStatus()
        {
            JsonElement response = SendRequest(nameof(GetStreamStatus));
            return JsonHelper.Deserialize<OutputStatus>(response);
        }

        /// <summary>
        /// Gets the status of the record output
        /// </summary>
        /// <returns>Current recording status</returns>
        public RecordingStatus GetRecordStatus()
        {
            JsonElement response = SendRequest(nameof(GetRecordStatus));
            return JsonHelper.Deserialize<RecordingStatus>(response);
        }

        /// <summary>
        /// Get the current transition name and duration
        /// </summary>
        /// <returns>TransitionSettings object with current transition info</returns>
        public TransitionSettings GetCurrentSceneTransition()
        {
            JsonElement response = SendRequest(nameof(GetCurrentSceneTransition));
            return JsonHelper.Deserialize<TransitionSettings>(response);
        }

        /// <summary>
        /// Change the transition that will be used
        /// </summary>
        /// <param name="transitionName">Desired transition name</param>
        public void SetCurrentSceneTransition(string transitionName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(transitionName), transitionName }
            };

            SendRequest(nameof(SetCurrentSceneTransition), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Change the transition duration
        /// </summary>
        /// <param name="transitionDuration">Desired transition duration (in milliseconds)</param>
        public void SetCurrentSceneTransitionDuration(int transitionDuration)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(transitionDuration), transitionDuration }
            };

            SendRequest(nameof(SetCurrentSceneTransitionDuration), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Change the transition settings (parameters specific to the transition type)
        /// </summary>
        /// <param name="transitionSettings">Transition settings (they can be partial)</param>
        /// <param name="overlay">Whether to overlay over the current settings or replace them</param>
        public void SetCurrentSceneTransitionSettings(JsonElement transitionSettings, bool overlay)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(transitionSettings), transitionSettings },
                { nameof(overlay), overlay }
            };

            SendRequest(nameof(SetCurrentSceneTransitionSettings), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Change the volume of the specified source
        /// </summary>
        /// <param name="inputName">Name of the source which volume will be changed</param>
        /// <param name="inputVolume">Desired volume. Must be between 0.0 and 1.0 for amplitude/mul (useDecibel is false), and under 0.0 for dB (useDecibel is true)</param>
        /// <param name="inputVolumeDb">Interpret volume data as decibels instead of amplitude/mul.</param>
        public void SetInputVolume(string inputName, float inputVolume, bool inputVolumeDb = false)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName }
            };

            if (inputVolumeDb)
            {
                requestData.Add(REQUEST_FIELD_VOLUME_DB, inputVolume);
            }
            else
            {
                requestData.Add(REQUEST_FIELD_VOLUME_MUL, inputVolume);
            }

            SendRequest(nameof(SetInputVolume), JsonHelper.ToJsonElement(requestData));
        }        /// <summary>
        /// Get the volume of the specified source
        /// </summary>
        /// <param name="inputName">Name of the source</param>
        /// <returns>An VolumeInfo object containing the volume levels</returns>
        public VolumeInfo GetInputVolume(string inputName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName }
            };

            JsonElement response = SendRequest(nameof(GetInputVolume), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.Deserialize<VolumeInfo>(response);
        }

        /// <summary>
        /// Get the mute status of the specified source
        /// </summary>
        /// <param name="inputName">Name of the source</param>
        /// <returns>Mute status of the source</returns>
        public bool GetInputMute(string inputName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName }
            };

            JsonElement response = SendRequest(nameof(GetInputMute), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.GetPropertyValue<bool>(response, "inputMuted", false);
        }

        /// <summary>
        /// Set the mute state of the specified source
        /// </summary>
        /// <param name="inputName">Name of the source which mute state will be changed</param>
        /// <param name="inputMuted">Desired mute state</param>
        public void SetInputMute(string inputName, bool inputMuted)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName },
                { nameof(inputMuted), inputMuted }
            };

            SendRequest(nameof(SetInputMute), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Toggle the mute state of the specified source
        /// </summary>
        /// <param name="inputName">Name of the source which mute state will be toggled</param>
        public void ToggleInputMute(string inputName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName }
            };

            SendRequest(nameof(ToggleInputMute), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Sets the audio sync offset of an input.
        /// </summary>
        /// <param name="inputName">Name of the input to set the audio sync offset of</param>
        /// <param name="inputAudioSyncOffset">New audio sync offset in milliseconds</param>
        public void SetInputAudioSyncOffset(string inputName, int inputAudioSyncOffset)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName },
                { nameof(inputAudioSyncOffset), inputAudioSyncOffset }
            };

            SendRequest(nameof(SetInputAudioSyncOffset), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Gets the audio sync offset of an input.
        /// </summary>
        /// <param name="inputName">Name of the input to get the audio sync offset of</param>
        /// <returns>Audio sync offset in milliseconds</returns>
        public int GetInputAudioSyncOffset(string inputName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName }
            };

            JsonElement response = SendRequest(nameof(GetInputAudioSyncOffset), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.GetPropertyValue<int>(response, "inputAudioSyncOffset", 0);
        }

        /// <summary>
        /// Gets the audio balance of an input.
        /// </summary>
        /// <param name="inputName">Name of the input to get the audio balance of</param>
        /// <returns>Audio balance value from 0.0-1.0</returns>
        public double GetInputAudioBalance(string inputName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName }
            };

            JsonElement response = SendRequest(nameof(GetInputAudioBalance), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.GetPropertyValue<double>(response, "inputAudioBalance", 0.5);
        }

        /// <summary>
        /// Sets the audio balance of an input.
        /// </summary>
        /// <param name="inputName">Name of the input to set the audio balance of</param>
        /// <param name="inputAudioBalance">New audio balance value</param>
        public void SetInputAudioBalance(string inputName, double inputAudioBalance)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName },
                { nameof(inputAudioBalance), inputAudioBalance }
            };

            SendRequest(nameof(SetInputAudioBalance), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Sets the transform and crop info of a scene item
        /// </summary>
        /// <param name="sceneName">Name of the scene containing the source</param>
        /// <param name="sceneItemId">Numeric ID of the scene item</param>
        /// <param name="sceneItemTransform">Scene item transform properties to update</param>
        public void SetSceneItemTransform(string sceneName, int sceneItemId, SceneItemTransformInfo sceneItemTransform)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sceneItemId), sceneItemId },
                { "sceneItemTransform", sceneItemTransform }
            };

            SendRequest(nameof(SetSceneItemTransform), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Sets the transform and crop info of a scene item
        /// </summary>
        /// <param name="sceneName">Name of the scene containing the source</param>
        /// <param name="sceneItemId">Numeric ID of the scene item</param>
        /// <param name="sceneItemTransform">Scene item transform properties as JsonElement</param>
        public void SetSceneItemTransform(string sceneName, int sceneItemId, JsonElement sceneItemTransform)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sceneItemId), sceneItemId },
                { "sceneItemTransform", sceneItemTransform }
            };

            SendRequest(nameof(SetSceneItemTransform), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Gets the transform and crop info of a scene item
        /// </summary>
        /// <param name="sceneName">Name of the scene containing the source</param>
        /// <param name="sceneItemId">Numeric ID of the scene item</param>
        /// <returns>Scene item transform info</returns>
        public SceneItemTransformInfo GetSceneItemTransform(string sceneName, int sceneItemId)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sceneItemId), sceneItemId }
            };

            JsonElement response = SendRequest(nameof(GetSceneItemTransform), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.Deserialize<SceneItemTransformInfo>(response.GetProperty("sceneItemTransform"));
        }

        /// <summary>
        /// Removes a scene item from a scene
        /// </summary>
        /// <param name="sceneName">Name of the scene</param>
        /// <param name="sceneItemId">Numeric ID of the scene item</param>
        public void RemoveSceneItem(string sceneName, int sceneItemId)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sceneItemId), sceneItemId }
            };

            SendRequest(nameof(RemoveSceneItem), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Gets the enable state of a scene item
        /// </summary>
        /// <param name="sceneName">Name of the scene containing the source</param>
        /// <param name="sceneItemId">Numeric ID of the scene item</param>
        /// <returns>Whether the scene item is enabled/visible</returns>
        public bool GetSceneItemEnabled(string sceneName, int sceneItemId)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sceneItemId), sceneItemId }
            };

            JsonElement response = SendRequest(nameof(GetSceneItemEnabled), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.GetPropertyValue<bool>(response, "sceneItemEnabled", false);
        }

        /// <summary>
        /// Sets the enable state of a scene item
        /// </summary>
        /// <param name="sceneName">Name of the scene containing the source</param>
        /// <param name="sceneItemId">Numeric ID of the scene item</param>
        /// <param name="sceneItemEnabled">New enable state of the scene item</param>
        public void SetSceneItemEnabled(string sceneName, int sceneItemId, bool sceneItemEnabled)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sceneItemId), sceneItemId },
                { nameof(sceneItemEnabled), sceneItemEnabled }
            };

            SendRequest(nameof(SetSceneItemEnabled), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Gets the lock state of a scene item
        /// </summary>
        /// <param name="sceneName">Name of the scene containing the source</param>
        /// <param name="sceneItemId">Numeric ID of the scene item</param>
        /// <returns>Whether the scene item is locked</returns>
        public bool GetSceneItemLocked(string sceneName, int sceneItemId)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sceneItemId), sceneItemId }
            };

            JsonElement response = SendRequest(nameof(GetSceneItemLocked), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.GetPropertyValue<bool>(response, "sceneItemLocked", false);
        }

        /// <summary>
        /// Sets the lock state of a scene item
        /// </summary>
        /// <param name="sceneName">Name of the scene containing the source</param>
        /// <param name="sceneItemId">Numeric ID of the scene item</param>
        /// <param name="sceneItemLocked">New lock state of the scene item</param>
        public void SetSceneItemLocked(string sceneName, int sceneItemId, bool sceneItemLocked)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sceneItemId), sceneItemId },
                { nameof(sceneItemLocked), sceneItemLocked }
            };

            SendRequest(nameof(SetSceneItemLocked), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Gets the index position of a scene item
        /// </summary>
        /// <param name="sceneName">Name of the scene containing the source</param>
        /// <param name="sceneItemId">Numeric ID of the scene item</param>
        /// <returns>Index position of the scene item</returns>
        public int GetSceneItemIndex(string sceneName, int sceneItemId)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sceneItemId), sceneItemId }
            };

            JsonElement response = SendRequest(nameof(GetSceneItemIndex), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.GetPropertyValue<int>(response, "sceneItemIndex", 0);
        }

        /// <summary>
        /// Sets the index position of a scene item
        /// </summary>
        /// <param name="sceneName">Name of the scene containing the source</param>
        /// <param name="sceneItemId">Numeric ID of the scene item</param>
        /// <param name="sceneItemIndex">New index position of the scene item</param>
        public void SetSceneItemIndex(string sceneName, int sceneItemId, int sceneItemIndex)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sceneItemId), sceneItemId },
                { nameof(sceneItemIndex), sceneItemIndex }
            };

            SendRequest(nameof(SetSceneItemIndex), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Set the current scene collection to the specified one
        /// </summary>
        /// <param name="sceneCollectionName">Desired scene collection name</param>
        public void SetCurrentSceneCollection(string sceneCollectionName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneCollectionName), sceneCollectionName }
            };

            SendRequest(nameof(SetCurrentSceneCollection), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Get the current scene collection name
        /// </summary>
        /// <returns>Name of the current scene collection</returns>
        public string GetCurrentSceneCollection()
        {
            JsonElement response = SendRequest(nameof(GetCurrentSceneCollection));
            return JsonHelper.GetStringValue(response, "currentSceneCollectionName");
        }

        /// <summary>
        /// List all scene collections
        /// </summary>
        /// <returns>A list of all scene collection names</returns>
        public List<string> GetSceneCollectionList()
        {
            JsonElement response = SendRequest(nameof(GetSceneCollectionList));
            var sceneCollections = new List<string>();
            
            if (response.TryGetProperty("sceneCollections", out var collectionsArray))
            {
                foreach (var item in collectionsArray.EnumerateArray())
                {
                    if (item.TryGetProperty("sceneCollectionName", out var nameElement))
                    {
                        sceneCollections.Add(nameElement.GetString());
                    }
                }
            }
            
            return sceneCollections;
        }

        /// <summary>
        /// Set the current profile to the specified one
        /// </summary>
        /// <param name="profileName">Name of the desired profile</param>
        public void SetCurrentProfile(string profileName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(profileName), profileName }
            };

            SendRequest(nameof(SetCurrentProfile), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// List all profiles
        /// </summary>
        /// <returns>A GetProfileListInfo object containing the names of all profiles</returns>
        public GetProfileListInfo GetProfileList()
        {
            JsonElement response = SendRequest(nameof(GetProfileList));
            return JsonHelper.Deserialize<GetProfileListInfo>(response);
        }

        /// <summary>
        /// Creates a new scene collection
        /// </summary>
        /// <param name="sceneCollectionName">Name for the new scene collection</param>
        public void CreateSceneCollection(string sceneCollectionName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneCollectionName), sceneCollectionName }
            };

            SendRequest(nameof(CreateSceneCollection), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Creates a new profile
        /// </summary>
        /// <param name="profileName">Name for the new profile</param>
        public void CreateProfile(string profileName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(profileName), profileName }
            };

            SendRequest(nameof(CreateProfile), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Removes a profile
        /// </summary>
        /// <param name="profileName">Name of the profile to remove</param>
        public void RemoveProfile(string profileName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(profileName), profileName }
            };

            SendRequest(nameof(RemoveProfile), JsonHelper.ToJsonElement(requestData));
        }        /// <summary>
        /// Gets a parameter from the current profile's configuration
        /// </summary>
        /// <param name="parameterCategory">Category of the parameter to get</param>
        /// <param name="parameterName">Name of the parameter to get</param>
        /// <returns>Value associated with the parameter</returns>
        public JsonElement GetProfileParameter(string parameterCategory, string parameterName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(parameterCategory), parameterCategory },
                { nameof(parameterName), parameterName }
            };

            JsonElement response = SendRequest(nameof(GetProfileParameter), JsonHelper.ToJsonElement(requestData));
            return response.TryGetProperty("parameterValue", out var value) ? value : new JsonElement();
        }

        /// <summary>
        /// Sets the value of a parameter in the current profile's configuration
        /// </summary>
        /// <param name="parameterCategory">Category of the parameter to set</param>
        /// <param name="parameterName">Name of the parameter to set</param>
        /// <param name="parameterValue">Value of the parameter to set</param>
        public void SetProfileParameter(string parameterCategory, string parameterName, string parameterValue)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(parameterCategory), parameterCategory },
                { nameof(parameterName), parameterName },
                { nameof(parameterValue), parameterValue }
            };

            SendRequest(nameof(SetProfileParameter), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Apply settings to a source filter
        /// </summary>
        /// <param name="sourceName">Source with filter</param>
        /// <param name="filterName">Filter name</param>
        /// <param name="filterSettings">JsonElement with filter settings</param>
        /// <param name="overlay">Apply over existing settings?</param>
        public void SetSourceFilterSettings(string sourceName, string filterName, JsonElement filterSettings, bool overlay = false)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sourceName), sourceName },
                { nameof(filterName), filterName },
                { nameof(filterSettings), filterSettings },
                { nameof(overlay), overlay }
            };

            SendRequest(nameof(SetSourceFilterSettings), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Apply settings to a source filter
        /// </summary>
        /// <param name="sourceName">Source with filter</param>
        /// <param name="filterName">Filter name</param>
        /// <param name="filterSettings">Filter settings</param>
        /// <param name="overlay">Apply over existing settings?</param>
        public void SetSourceFilterSettings(string sourceName, string filterName, FilterSettings filterSettings, bool overlay = false)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sourceName), sourceName },
                { nameof(filterName), filterName },
                { nameof(filterSettings), filterSettings },
                { nameof(overlay), overlay }
            };

            SendRequest(nameof(SetSourceFilterSettings), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Modify the Source Filter's visibility
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="filterName">Source filter name</param>
        /// <param name="filterEnabled">New filter state</param>
        public void SetSourceFilterEnabled(string sourceName, string filterName, bool filterEnabled)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sourceName), sourceName },
                { nameof(filterName), filterName },
                { nameof(filterEnabled), filterEnabled }
            };

            SendRequest(nameof(SetSourceFilterEnabled), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Return a list of all filters on a source
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <returns>List of filters</returns>
        public List<FilterSettings> GetSourceFilterList(string sourceName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sourceName), sourceName }
            };

            JsonElement response = SendRequest(nameof(GetSourceFilterList), JsonHelper.ToJsonElement(requestData));
            var filters = new List<FilterSettings>();

            if (response.TryGetProperty("filters", out var filtersArray))
            {
                foreach (var filterElement in filtersArray.EnumerateArray())
                {
                    filters.Add(JsonHelper.Deserialize<FilterSettings>(filterElement));
                }
            }

            return filters;
        }

        /// <summary>
        /// Get settings of a source filter
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="filterName">Source filter name</param>
        /// <returns>Filter settings</returns>
        public FilterSettings GetSourceFilter(string sourceName, string filterName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sourceName), sourceName },
                { nameof(filterName), filterName }
            };

            JsonElement response = SendRequest(nameof(GetSourceFilter), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.Deserialize<FilterSettings>(response);
        }        /// <summary>
        /// Remove a filter from a source
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="filterName">Name of the filter</param>
        public bool RemoveSourceFilter(string sourceName, string filterName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sourceName), sourceName },
                { nameof(filterName), filterName }
            };

            SendRequest(nameof(RemoveSourceFilter), JsonHelper.ToJsonElement(requestData));
            return true; // If no exception was thrown, the operation was successful
        }

        /// <summary>
        /// Add a new filter to a source
        /// </summary>
        /// <param name="sourceName">Name of the source on which the filter is added</param>
        /// <param name="filterName">Name of the new filter</param>
        /// <param name="filterKind">Filter type</param>
        /// <param name="filterSettings">JsonElement with filter settings</param>
        public void CreateSourceFilter(string sourceName, string filterName, string filterKind, JsonElement filterSettings)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sourceName), sourceName },
                { nameof(filterName), filterName },
                { nameof(filterKind), filterKind },
                { nameof(filterSettings), filterSettings }
            };

            SendRequest(nameof(CreateSourceFilter), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Add a new filter to a source
        /// </summary>
        /// <param name="sourceName">Name of the source on which the filter is added</param>
        /// <param name="filterName">Name of the new filter</param>
        /// <param name="filterKind">Filter type</param>
        /// <param name="filterSettings">Filter settings object</param>
        public void CreateSourceFilter(string sourceName, string filterName, string filterKind, FilterSettings filterSettings)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sourceName), sourceName },
                { nameof(filterName), filterName },
                { nameof(filterKind), filterKind },
                { nameof(filterSettings), filterSettings }
            };

            SendRequest(nameof(CreateSourceFilter), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Gets the default settings for a filter kind
        /// </summary>
        /// <param name="filterKind">Filter kind to get the default settings for</param>
        /// <returns>Object of default settings for the filter kind</returns>
        public JsonElement GetSourceFilterDefaultSettings(string filterKind)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(filterKind), filterKind }
            };

            JsonElement response = SendRequest(nameof(GetSourceFilterDefaultSettings), JsonHelper.ToJsonElement(requestData));
            return response.GetProperty("defaultFilterSettings");
        }

        /// <summary>
        /// Sets the name of a source filter (rename)
        /// </summary>
        /// <param name="sourceName">Name of the source the filter is on</param>
        /// <param name="filterName">Current name of the filter</param>
        /// <param name="newFilterName">New name for the filter</param>
        public void SetSourceFilterName(string sourceName, string filterName, string newFilterName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sourceName), sourceName },
                { nameof(filterName), filterName },
                { "newFilterName", newFilterName }
            };

            SendRequest(nameof(SetSourceFilterName), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Sets the index position of a filter on a source
        /// </summary>
        /// <param name="sourceName">Name of the source the filter is on</param>
        /// <param name="filterName">Name of the filter</param>
        /// <param name="filterIndex">New index position of the filter</param>
        public void SetSourceFilterIndex(string sourceName, string filterName, int filterIndex)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sourceName), sourceName },
                { nameof(filterName), filterName },
                { nameof(filterIndex), filterIndex }
            };

            SendRequest(nameof(SetSourceFilterIndex), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Gets the status of the studio mode.
        /// </summary>
        /// <returns>Studio Mode status (on/off)</returns>
        public bool GetStudioModeEnabled()
        {
            JsonElement response = SendRequest(nameof(GetStudioModeEnabled));
            return JsonHelper.GetPropertyValue<bool>(response, "studioModeEnabled", false);
        }

        /// <summary>
        /// Enables or disables studio mode
        /// </summary>
        /// <param name="studioModeEnabled">True to enable studio mode, false to disable</param>
        public void SetStudioModeEnabled(bool studioModeEnabled)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(studioModeEnabled), studioModeEnabled }
            };

            SendRequest(nameof(SetStudioModeEnabled), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Get the name of the currently selected preview scene. 
        /// Note: Triggers an error if Studio Mode is disabled
        /// </summary>
        /// <returns>Preview scene name</returns>
        public string GetCurrentPreviewScene()
        {
            JsonElement response = SendRequest(nameof(GetCurrentPreviewScene));
            return JsonHelper.GetStringValue(response, "currentPreviewSceneName");
        }

        /// <summary>
        /// Change the currently active preview/studio scene to the one specified.
        /// Triggers an error if Studio Mode is disabled
        /// </summary>
        /// <param name="sceneName">Preview scene name</param>
        public void SetCurrentPreviewScene(string sceneName)
        {
            var requestData = new Dictionary<string, object>
            {
                { "sceneName", sceneName }
            };

            SendRequest(nameof(SetCurrentPreviewScene), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Change the currently active preview/studio scene to the one specified.
        /// Triggers an error if Studio Mode is disabled.
        /// </summary>
        /// <param name="previewScene">Preview scene object</param>
        public void SetCurrentPreviewScene(ObsScene previewScene)
        {
            SetCurrentPreviewScene(previewScene.Name);
        }

        /// <summary>
        /// Triggers the current scene transition. Same functionality as the `Transition` button in Studio Mode
        /// </summary>
        public void TriggerStudioModeTransition()
        {
            SendRequest(nameof(TriggerStudioModeTransition));
        }

        /// <summary>
        /// Get the status of the OBS replay buffer.
        /// </summary>
        /// <returns>Current replay buffer status. true when active</returns>
        public bool GetReplayBufferStatus()
        {
            JsonElement response = SendRequest(nameof(GetReplayBufferStatus));
            return JsonHelper.GetPropertyValue<bool>(response, "outputActive", false);
        }

        /// <summary>
        /// Toggles the state of the replay buffer output.
        /// </summary>
        public void ToggleReplayBuffer()
        {
            SendRequest(nameof(ToggleReplayBuffer));
        }

        /// <summary>
        /// Start recording into the Replay Buffer. Triggers an error
        /// if the Replay Buffer is already active, or if the "Save Replay Buffer"
        /// hotkey is not set in OBS' settings
        /// </summary>
        public void StartReplayBuffer()
        {
            SendRequest(nameof(StartReplayBuffer));
        }

        /// <summary>
        /// Stop recording into the Replay Buffer. Triggers an error if the
        /// Replay Buffer is not active.
        /// </summary>
        public void StopReplayBuffer()
        {
            SendRequest(nameof(StopReplayBuffer));
        }

        /// <summary>
        /// Save and flush the contents of the Replay Buffer to disk. Basically
        /// the same as triggering the "Save Replay Buffer" hotkey in OBS.
        /// Triggers an error if Replay Buffer is not active.
        /// </summary>
        public void SaveReplayBuffer()
        {
            SendRequest(nameof(SaveReplayBuffer));
        }

        /// <summary>
        /// Gets the filename of the last replay buffer save file.
        /// </summary>
        /// <returns>File path of last replay</returns>
        public string GetLastReplayBufferReplay()
        {
            JsonElement response = SendRequest(nameof(GetLastReplayBufferReplay));
            return JsonHelper.GetStringValue(response, "savedReplayPath");
        }

        /// <summary>
        /// Get an array of all scene transitions available to OBS
        /// </summary>
        /// <returns>GetTransitionListInfo object with current transition info</returns>
        public GetTransitionListInfo GetSceneTransitionList()
        {
            JsonElement response = SendRequest(nameof(GetSceneTransitionList));
            return JsonHelper.Deserialize<GetTransitionListInfo>(response);
        }

        /// <summary>
        /// Gets an array of all available transition kinds.
        /// Similar to `GetInputKindList`
        /// </summary>
        /// <returns>Array of transition kinds</returns>
        public List<string> GetTransitionKindList()
        {
            JsonElement response = SendRequest(nameof(GetTransitionKindList));
            var transitionKinds = new List<string>();

            if (response.TryGetProperty("transitionKinds", out var kindsArray))
            {
                foreach (var item in kindsArray.EnumerateArray())
                {
                    transitionKinds.Add(item.GetString());
                }
            }

            return transitionKinds;
        }

        /// <summary>
        /// Gets the cursor position of the current scene transition.
        /// Note: `transitionCursor` will return 1.0 when the transition is inactive.
        /// </summary>
        /// <returns>Cursor position, between 0.0 and 1.0</returns>
        public double GetCurrentSceneTransitionCursor()
        {
            JsonElement response = SendRequest(nameof(GetCurrentSceneTransitionCursor));
            return JsonHelper.GetPropertyValue<double>(response, "transitionCursor", 1.0);
        }

        /// <summary>
        /// Gets an array of all hotkey names in OBS
        /// </summary>
        /// <returns>List of hotkey names</returns>
        public List<string> GetHotkeyList()
        {
            JsonElement response = SendRequest(nameof(GetHotkeyList));
            var hotkeys = new List<string>();

            if (response.TryGetProperty("hotkeys", out var hotkeysArray))
            {
                foreach (var item in hotkeysArray.EnumerateArray())
                {
                    hotkeys.Add(item.GetString());
                }
            }

            return hotkeys;
        }

        /// <summary>
        /// Gets an array of all inputs in OBS.
        /// </summary>
        /// <param name="inputKind">Restrict the array to only inputs of the specified kind</param>
        /// <returns>Array of inputs</returns>
        public List<InputBasicInfo> GetInputList(string inputKind = null)
        {
            var requestData = new Dictionary<string, object>();
            
            if (!string.IsNullOrEmpty(inputKind))
            {
                requestData.Add(nameof(inputKind), inputKind);
            }

            JsonElement response = SendRequest(nameof(GetInputList), requestData.Count > 0 ? JsonHelper.ToJsonElement(requestData) : new JsonElement());
            var inputs = new List<InputBasicInfo>();

            if (response.TryGetProperty("inputs", out var inputsArray))
            {
                foreach (var inputElement in inputsArray.EnumerateArray())
                {
                    inputs.Add(JsonHelper.Deserialize<InputBasicInfo>(inputElement));
                }
            }

            return inputs;
        }

        /// <summary>
        /// Gets an array of all available input kinds in OBS.
        /// </summary>
        /// <param name="unversioned">Return unversioned input kinds (true) or versioned input kinds (false)</param>
        /// <returns>Array of input kinds</returns>
        public List<string> GetInputKindList(bool unversioned = true)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(unversioned), unversioned }
            };

            JsonElement response = SendRequest(nameof(GetInputKindList), JsonHelper.ToJsonElement(requestData));
            var inputKinds = new List<string>();

            if (response.TryGetProperty("inputKinds", out var kindsArray))
            {
                foreach (var item in kindsArray.EnumerateArray())
                {
                    inputKinds.Add(item.GetString());
                }
            }

            return inputKinds;
        }

        /// <summary>
        /// Creates a new input
        /// </summary>
        /// <param name="sceneName">Name of the scene to add the input to as a scene item</param>
        /// <param name="inputName">Name of the new input to created</param>
        /// <param name="inputKind">The kind of input to be created</param>
        /// <param name="inputSettings">Settings object to initialize the input with</param>
        /// <param name="sceneItemEnabled">Whether to set the created scene item to enabled or disabled</param>
        /// <returns>ID of the created scene item</returns>
        public int CreateInput(string sceneName, string inputName, string inputKind, JsonElement inputSettings = default, bool sceneItemEnabled = true)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(inputName), inputName },
                { nameof(inputKind), inputKind },
                { nameof(sceneItemEnabled), sceneItemEnabled }
            };

            if (inputSettings.ValueKind != JsonValueKind.Undefined)
            {
                requestData.Add(nameof(inputSettings), inputSettings);
            }

            JsonElement response = SendRequest(nameof(CreateInput), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.GetPropertyValue<int>(response, "sceneItemId", 0);
        }

        /// <summary>
        /// Removes an existing input
        /// </summary>
        /// <param name="inputName">Name of the existing input to remove</param>
        public void RemoveInput(string inputName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName }
            };

            SendRequest(nameof(RemoveInput), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Sets the name of an input (rename)
        /// </summary>
        /// <param name="inputName">Current input name</param>
        /// <param name="newInputName">New name for the input</param>
        public void SetInputName(string inputName, string newInputName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName },
                { nameof(newInputName), newInputName }
            };

            SendRequest(nameof(SetInputName), JsonHelper.ToJsonElement(requestData));
        }        /// <summary>
        /// Gets the settings of an input
        /// </summary>
        /// <param name="inputName">Name of the input to get the settings of</param>
        /// <returns>Object of input settings</returns>
        public InputSettings GetInputSettings(string inputName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName }
            };

            JsonElement response = SendRequest(nameof(GetInputSettings), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.Deserialize<InputSettings>(response);
        }

        /// <summary>
        /// Sets the settings of an input
        /// </summary>
        /// <param name="inputName">Name of the input to set the settings of</param>
        /// <param name="inputSettings">Object of input settings to apply</param>
        /// <param name="overlay">True to apply the settings on top of existing ones, false to reset the input to its defaults first</param>
        public void SetInputSettings(string inputName, JsonElement inputSettings, bool overlay = true)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName },
                { nameof(inputSettings), inputSettings },
                { nameof(overlay), overlay }
            };

            SendRequest(nameof(SetInputSettings), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Gets the audio tracks associated with an input.
        /// </summary>
        /// <param name="inputName">Name of the input</param>
        /// <returns>Object of audio tracks and associated enable states</returns>
        public SourceTracks GetInputAudioTracks(string inputName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName }
            };

            JsonElement response = SendRequest(nameof(GetInputAudioTracks), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.Deserialize<SourceTracks>(response.GetProperty("inputAudioTracks"));
        }

        /// <summary>
        /// Sets the enable state of audio tracks of an input.
        /// </summary>
        /// <param name="inputName">Name of the input</param>
        /// <param name="inputAudioTracks">Track settings to apply</param>
        public void SetInputAudioTracks(string inputName, SourceTracks inputAudioTracks)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName },
                { nameof(inputAudioTracks), inputAudioTracks }
            };

            SendRequest(nameof(SetInputAudioTracks), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Gets the items of a scene
        /// </summary>
        /// <param name="sceneName">Name of the scene to get the items of</param>
        /// <returns>Array of scene items</returns>
        public List<SceneItemDetails> GetSceneItemList(string sceneName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName }
            };

            JsonElement response = SendRequest(nameof(GetSceneItemList), JsonHelper.ToJsonElement(requestData));
            var sceneItems = new List<SceneItemDetails>();

            if (response.TryGetProperty("sceneItems", out var itemsArray))
            {
                foreach (var itemElement in itemsArray.EnumerateArray())
                {
                    sceneItems.Add(JsonHelper.Deserialize<SceneItemDetails>(itemElement));
                }
            }

            return sceneItems;
        }

        /// <summary>
        /// Basically GetSceneItemList, but only returns a single scene item.
        /// </summary>
        /// <param name="sceneName">Name of the scene to search in</param>
        /// <param name="sceneItemId">Numeric ID of the scene item</param>
        /// <returns>Scene item info</returns>
        public SceneItemDetails GetSceneItemById(string sceneName, int sceneItemId)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sceneItemId), sceneItemId }
            };

            JsonElement response = SendRequest(nameof(GetSceneItemById), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.Deserialize<SceneItemDetails>(response);
        }

        /// <summary>
        /// Searches a scene for a source, and returns its id.
        /// </summary>
        /// <param name="sceneName">Name of the scene to search in</param>
        /// <param name="sourceName">Name of the source to find</param>
        /// <param name="searchOffset">Number of matches to skip during search. >= 0 means first forward from start, -1 means last (top) item</param>
        /// <returns>Numeric ID of the scene item</returns>
        public int GetSceneItemId(string sceneName, string sourceName, int searchOffset = 0)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sourceName), sourceName },
                { nameof(searchOffset), searchOffset }
            };

            JsonElement response = SendRequest(nameof(GetSceneItemId), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.GetPropertyValue<int>(response, "sceneItemId", 0);
        }

        /// <summary>
        /// Creates a new scene item using a source.
        /// </summary>
        /// <param name="sceneName">Name of the scene to create the item in</param>
        /// <param name="sourceName">Name of the source to add to the scene</param>
        /// <param name="sceneItemEnabled">Enable state to apply to the scene item on creation</param>
        /// <returns>Numeric ID of the created scene item</returns>
        public int CreateSceneItem(string sceneName, string sourceName, bool sceneItemEnabled = true)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sourceName), sourceName },
                { nameof(sceneItemEnabled), sceneItemEnabled }
            };

            JsonElement response = SendRequest(nameof(CreateSceneItem), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.GetPropertyValue<int>(response, "sceneItemId", 0);
        }

        /// <summary>
        /// Creates a new scene in OBS.
        /// </summary>
        /// <param name="sceneName">Name for the new scene</param>
        public void CreateScene(string sceneName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName }
            };

            SendRequest(nameof(CreateScene), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Removes a scene from OBS.
        /// </summary>
        /// <param name="sceneName">Name of the scene to remove</param>
        public void RemoveScene(string sceneName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName }
            };

            SendRequest(nameof(RemoveScene), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Sets the name of a scene (rename).
        /// </summary>
        /// <param name="sceneName">Current scene name</param>
        /// <param name="newSceneName">New name for the scene</param>
        public void SetSceneName(string sceneName, string newSceneName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(newSceneName), newSceneName }
            };

            SendRequest(nameof(SetSceneName), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Gets the blend mode of a scene item.
        /// </summary>
        /// <param name="sceneName">Name of the scene the item is in</param>
        /// <param name="sceneItemId">Numeric ID of the scene item</param>
        /// <returns>Current blend mode</returns>
        public string GetSceneItemBlendMode(string sceneName, int sceneItemId)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sceneItemId), sceneItemId }
            };

            JsonElement response = SendRequest(nameof(GetSceneItemBlendMode), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.GetStringValue(response, "sceneItemBlendMode");
        }

        /// <summary>
        /// Sets the blend mode of a scene item.
        /// </summary>
        /// <param name="sceneName">Name of the scene the item is in</param>
        /// <param name="sceneItemId">Numeric ID of the scene item</param>
        /// <param name="sceneItemBlendMode">New blend mode</param>
        public void SetSceneItemBlendMode(string sceneName, int sceneItemId, string sceneItemBlendMode)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sceneItemId), sceneItemId },
                { nameof(sceneItemBlendMode), sceneItemBlendMode }
            };

            SendRequest(nameof(SetSceneItemBlendMode), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Gets the status of the virtualcam output.
        /// </summary>
        /// <returns>Virtual camera status info</returns>
        public VirtualCamStatus GetVirtualCamStatus()
        {
            JsonElement response = SendRequest(nameof(GetVirtualCamStatus));
            return JsonHelper.Deserialize<VirtualCamStatus>(response);
        }        /// <summary>
        /// Toggles the state of the virtualcam output.
        /// </summary>
        public VirtualCamStatus ToggleVirtualCam()
        {
            JsonElement response = SendRequest(nameof(ToggleVirtualCam));
            return JsonHelper.Deserialize<VirtualCamStatus>(response);
        }

        /// <summary>
        /// Starts the virtualcam output.
        /// </summary>
        public void StartVirtualCam()
        {
            SendRequest(nameof(StartVirtualCam));
        }

        /// <summary>
        /// Stops the virtualcam output.
        /// </summary>
        public void StopVirtualCam()
        {
            SendRequest(nameof(StopVirtualCam));
        }

        /// <summary>
        /// Gets the status of a media input.
        /// </summary>
        /// <param name="inputName">Name of the media input</param>
        /// <returns>Status of the media input</returns>
        public MediaInputStatus GetMediaInputStatus(string inputName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName }
            };

            JsonElement response = SendRequest(nameof(GetMediaInputStatus), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.Deserialize<MediaInputStatus>(response);
        }

        /// <summary>
        /// Sends CEA-608 caption text over the stream output.
        /// </summary>
        /// <param name="captionText">Caption text to be sent</param>
        public void SendStreamCaption(string captionText)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(captionText), captionText }
            };

            SendRequest(nameof(SendStreamCaption), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Duplicates a scene item, copying all transform and crop info.
        /// </summary>
        /// <param name="sceneName">Name of the scene the item is in</param>
        /// <param name="sceneItemId">Numeric ID of the scene item</param>
        /// <param name="destinationSceneName">Name of the scene to create the duplicated item in</param>
        /// <returns>Numeric ID of the duplicated scene item</returns>
        public int DuplicateSceneItem(string sceneName, int sceneItemId, string destinationSceneName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sceneItemId), sceneItemId }
            };

            if (!string.IsNullOrEmpty(destinationSceneName))
            {
                requestData[nameof(destinationSceneName)] = destinationSceneName;
            }

            JsonElement response = SendRequest(nameof(DuplicateSceneItem), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.GetPropertyValue<int>(response, "sceneItemId", 0);
        }

        /// <summary>
        /// Gets an object of "special" inputs.
        /// </summary>
        /// <returns>Object of special inputs</returns>
        public Dictionary<string, string> GetSpecialInputs()
        {
            JsonElement response = SendRequest(nameof(GetSpecialInputs));
            var specialInputs = new Dictionary<string, string>();

            if (response.TryGetProperty("desktop1", out var desktop1Element))
            {
                specialInputs["desktop1"] = desktop1Element.GetString();
            }
            if (response.TryGetProperty("desktop2", out var desktop2Element))
            {
                specialInputs["desktop2"] = desktop2Element.GetString();
            }
            if (response.TryGetProperty("mic1", out var mic1Element))
            {
                specialInputs["mic1"] = mic1Element.GetString();
            }
            if (response.TryGetProperty("mic2", out var mic2Element))
            {
                specialInputs["mic2"] = mic2Element.GetString();
            }
            if (response.TryGetProperty("mic3", out var mic3Element))
            {
                specialInputs["mic3"] = mic3Element.GetString();
            }
            if (response.TryGetProperty("mic4", out var mic4Element))
            {
                specialInputs["mic4"] = mic4Element.GetString();
            }

            return specialInputs;
        }

        /// <summary>
        /// Sets the settings of a stream service.
        /// </summary>
        /// <param name="streamServiceSettings">Object containing stream service settings</param>
        public void SetStreamServiceSettings(StreamingService streamServiceSettings)
        {
            var requestData = new Dictionary<string, object>
            {
                { "streamServiceType", streamServiceSettings.Type },
                { "streamServiceSettings", streamServiceSettings.Settings }
            };

            SendRequest(nameof(SetStreamServiceSettings), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Gets the current stream service settings (stream destination).
        /// </summary>
        /// <returns>Current streaming service settings</returns>
        public StreamingService GetStreamServiceSettings()
        {
            JsonElement response = SendRequest(nameof(GetStreamServiceSettings));
            return new StreamingService(response);
        }

        /// <summary>
        /// Gets the audio monitor type of an input.
        /// </summary>
        /// <param name="inputName">Name of the input to get the audio monitor type of</param>
        /// <returns>Audio monitor type</returns>
        public string GetInputAudioMonitorType(string inputName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName }
            };

            JsonElement response = SendRequest(nameof(GetInputAudioMonitorType), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.GetPropertyValue<string>(response, "monitorType", "");
        }

        /// <summary>
        /// Sets the audio monitor type of an input.
        /// </summary>
        /// <param name="inputName">Name of the input to set the audio monitor type of</param>
        /// <param name="monitorType">Audio monitor type</param>
        public void SetInputAudioMonitorType(string inputName, string monitorType)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName },
                { nameof(monitorType), monitorType }
            };

            SendRequest(nameof(SetInputAudioMonitorType), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Sets the cursor position of a media input.
        /// </summary>
        /// <param name="inputName">Name of the media input</param>
        /// <param name="mediaCursor">New cursor position to set</param>
        public void SetMediaInputCursor(string inputName, int mediaCursor)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName },
                { nameof(mediaCursor), mediaCursor }
            };

            SendRequest(nameof(SetMediaInputCursor), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Offsets the current cursor position of a media input by the specified value.
        /// </summary>
        /// <param name="inputName">Name of the media input</param>
        /// <param name="mediaCursorOffset">Value to offset the current cursor position by</param>
        public void OffsetMediaInputCursor(string inputName, int mediaCursorOffset)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName },
                { nameof(mediaCursorOffset), mediaCursorOffset }
            };

            SendRequest(nameof(OffsetMediaInputCursor), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Creates a new input, adding it as a scene item to the specified scene.
        /// </summary>
        /// <param name="sceneName">Name of the scene to add the input to as a scene item</param>
        /// <param name="inputName">Name of the new input to created</param>
        /// <param name="inputKind">The kind of input to be created</param>
        /// <param name="inputSettings">Settings object to initialize the input with</param>
        /// <param name="sceneItemEnabled">Whether to set the created scene item to enabled or disabled</param>
        /// <returns>Numeric ID of the created scene item</returns>
        public int CreateInput(string sceneName, string inputName, string inputKind, JsonElement inputSettings, bool? sceneItemEnabled = null)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(inputName), inputName },
                { nameof(inputKind), inputKind },
                { nameof(inputSettings), inputSettings }
            };

            if (sceneItemEnabled.HasValue)
            {
                requestData[nameof(sceneItemEnabled)] = sceneItemEnabled.Value;
            }

            JsonElement response = SendRequest(nameof(CreateInput), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.GetPropertyValue<int>(response, "sceneItemId", 0);
        }

        /// <summary>
        /// Gets the default settings for an input kind.
        /// </summary>
        /// <param name="inputKind">Input kind to get the default settings for</param>
        /// <returns>Object of default settings for the input kind</returns>
        public JsonElement GetInputDefaultSettings(string inputKind)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputKind), inputKind }
            };

            JsonElement response = SendRequest(nameof(GetInputDefaultSettings), JsonHelper.ToJsonElement(requestData));
            return response.TryGetProperty("defaultInputSettings", out var settings) ? settings : new JsonElement();
        }

        /// <summary>
        /// Sets the audio tracks of an input.
        /// </summary>
        /// <param name="inputName">Name of the input</param>
        /// <param name="inputAudioTracks">Track settings to apply</param>
        public void SetInputAudioTracks(string inputName, JsonElement inputAudioTracks)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName },
                { nameof(inputAudioTracks), inputAudioTracks }
            };

            SendRequest(nameof(SetInputAudioTracks), JsonHelper.ToJsonElement(requestData));
        }        /// <summary>
        /// Gets the enable state of a source.
        /// </summary>
        /// <param name="sourceName">Name of the source to get the enable state of</param>
        /// <returns>Whether the source is showing in Program</returns>
        public SourceActiveInfo GetSourceActive(string sourceName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sourceName), sourceName }
            };

            JsonElement response = SendRequest(nameof(GetSourceActive), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.Deserialize<SourceActiveInfo>(response);
        }/// <summary>
        /// Gets some data about the current virtual cam output.
        /// </summary>
        /// <returns>Current virtual camera status</returns>
        public VirtualCamStatus ToggleVirtualCamStatus()
        {
            JsonElement response = SendRequest("ToggleVirtualCam");
            return new VirtualCamStatus(response);
        }

        /// <summary>
        /// Gets the value of a "slot" from the selected profile's persistent data.
        /// </summary>
        /// <param name="realm">The data realm to select</param>
        /// <param name="slotName">The name of the slot to retrieve data from</param>
        /// <returns>Value associated with the slot</returns>
        public JsonElement GetPersistentData(string realm, string slotName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(realm), realm },
                { nameof(slotName), slotName }
            };

            JsonElement response = SendRequest(nameof(GetPersistentData), JsonHelper.ToJsonElement(requestData));
            return response.TryGetProperty("slotValue", out var value) ? value : new JsonElement();
        }        /// <summary>
        /// Sets the value of a "slot" from the selected profile's persistent data.
        /// </summary>
        /// <param name="realm">The data realm to select</param>
        /// <param name="slotName">The name of the slot to set data for</param>
        /// <param name="slotValue">The value to apply to the slot</param>
        public void SetPersistentData(string realm, string slotName, JsonElement slotValue)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(realm), realm },
                { nameof(slotName), slotName },
                { nameof(slotValue), slotValue }
            };

            SendRequest(nameof(SetPersistentData), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Get the path of the current recording folder
        /// </summary>
        /// <returns>Current recording folder path</returns>
        public string GetRecordDirectory()
        {
            JsonElement response = SendRequest(nameof(GetRecordDirectory));
            return JsonHelper.GetPropertyValue<string>(response, "recordDirectory", "");
        }

        /// <summary>
        /// Broadcasts a custom event to all WebSocket clients.
        /// </summary>
        /// <param name="eventData">Object containing the event data</param>
        public void BroadcastCustomEvent(JsonElement eventData)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(eventData), eventData }
            };

            SendRequest(nameof(BroadcastCustomEvent), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Sets the current video settings.
        /// </summary>
        /// <param name="obsVideoSettings">Object of video settings to apply</param>
        public void SetVideoSettings(ObsVideoSettings obsVideoSettings)
        {
            var requestData = new Dictionary<string, object>
            {
                { "videoSettings", obsVideoSettings }
            };

            SendRequest(nameof(SetVideoSettings), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Gets data about the current plugin and RPC version.
        /// </summary>
        /// <returns>Version info in an ObsVersion object</returns>
        public ObsVersion GetVersion()
        {
            JsonElement response = SendRequest(nameof(GetVersion));
            return JsonHelper.Deserialize<ObsVersion>(response);
        }

        /// <summary>
        /// Call a request registered to a vendor.
        /// </summary>
        /// <param name="vendorName">Name of the vendor to use</param>
        /// <param name="requestType">The request type to call</param>
        /// <param name="requestData">Object containing appropriate request data</param>
        /// <returns>Object containing appropriate response data</returns>
        public JsonElement CallVendorRequest(string vendorName, string requestType, JsonElement? requestData = null)
        {
            var requestDict = new Dictionary<string, object>
            {
                { nameof(vendorName), vendorName },
                { nameof(requestType), requestType }
            };

            if (requestData.HasValue)
            {
                requestDict[nameof(requestData)] = requestData.Value;
            }

            JsonElement response = SendRequest(nameof(CallVendorRequest), JsonHelper.ToJsonElement(requestDict));
            return response.TryGetProperty("responseData", out var responseDataElement) ? responseDataElement : new JsonElement();
        }

        /// <summary>
        /// Sleeps for a time duration or number of frames.
        /// </summary>
        /// <param name="sleepMillis">Number of milliseconds to sleep for</param>
        /// <param name="sleepFrames">Number of frames to sleep for</param>
        public void Sleep(int sleepMillis, int sleepFrames)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sleepMillis), sleepMillis },
                { nameof(sleepFrames), sleepFrames }
            };

            SendRequest(nameof(Sleep), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Sets the settings of an input.
        /// </summary>
        /// <param name="inputSettings">Object of settings to apply</param>
        /// <param name="overlay">True == apply the settings on top of existing ones, False == reset the input to its defaults, then apply settings.</param>
        public void SetInputSettings(InputSettings inputSettings, bool overlay = true)
        {
            var requestData = new Dictionary<string, object>
            {
                { "inputName", inputSettings.InputName },
                { "inputSettings", inputSettings.Settings },
                { nameof(overlay), overlay }
            };

            SendRequest(nameof(SetInputSettings), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Gets the items of a list property from an input's properties.
        /// </summary>
        /// <param name="inputName">Name of the input</param>
        /// <param name="propertyName">Name of the list property to get the items of</param>
        /// <returns>Array of items in the list property</returns>
        public List<JsonElement> GetInputPropertiesListPropertyItems(string inputName, string propertyName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName },
                { nameof(propertyName), propertyName }
            };

            JsonElement response = SendRequest(nameof(GetInputPropertiesListPropertyItems), JsonHelper.ToJsonElement(requestData));
            var items = new List<JsonElement>();
            
            if (response.TryGetProperty("propertyItems", out var propertyItems) && propertyItems.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in propertyItems.EnumerateArray())
                {
                    items.Add(item);
                }
            }

            return items;
        }

        /// <summary>
        /// Presses a button in the properties of an input.
        /// </summary>
        /// <param name="inputName">Name of the input</param>
        /// <param name="propertyName">Name of the button property to press</param>
        public void PressInputPropertiesButton(string inputName, string propertyName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName },
                { nameof(propertyName), propertyName }
            };

            SendRequest(nameof(PressInputPropertiesButton), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Triggers an action on a media input.
        /// </summary>
        /// <param name="inputName">Name of the media input</param>
        /// <param name="mediaAction">Identifier of the ObsMediaInputAction enum</param>
        public void TriggerMediaInputAction(string inputName, string mediaAction)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName },
                { nameof(mediaAction), mediaAction }
            };

            SendRequest(nameof(TriggerMediaInputAction), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Toggles pause on the record output.
        /// </summary>
        public void ToggleRecordPause()
        {
            SendRequest(nameof(ToggleRecordPause));
        }

        /// <summary>
        /// Basically GetSceneItemList, but for groups.
        /// Using groups at all in OBS is discouraged, as they are very broken under the hood.
        /// Groups only
        /// </summary>
        /// <param name="sceneName">Name of the group to get the items of</param>
        /// <returns>Array of scene items in the group</returns>
        public List<JsonElement> GetGroupSceneItemList(string sceneName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName }
            };

            JsonElement response = SendRequest(nameof(GetGroupSceneItemList), JsonHelper.ToJsonElement(requestData));
            var items = new List<JsonElement>();
            
            if (response.TryGetProperty("sceneItems", out var sceneItems) && sceneItems.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in sceneItems.EnumerateArray())
                {
                    items.Add(item);
                }
            }

            return items;
        }

        /// <summary>
        /// Gets the JsonElement of transform settings for a scene item. Use this one you don't want it populated with default values.
        /// Scenes and Groups
        /// </summary>
        /// <param name="sceneName">Name of the scene the item is in</param>
        /// <param name="sceneItemId">Numeric ID of the scene item</param>
        /// <returns>Object containing scene item transform info</returns>
        public JsonElement GetSceneItemTransformRaw(string sceneName, int sceneItemId)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sceneName), sceneName },
                { nameof(sceneItemId), sceneItemId }
            };

            JsonElement response = SendRequest(nameof(GetSceneItemTransform), JsonHelper.ToJsonElement(requestData));
            return response.TryGetProperty("sceneItemTransform", out var transform) ? transform : new JsonElement();
        }

        /// <summary>
        /// Gets an array of all groups in OBS.
        /// Groups in OBS are actually scenes, but renamed and modified. In obs-websocket, we treat them as scenes where we can.
        /// </summary>
        /// <returns>Array of group names</returns>
        public List<string> GetGroupList()
        {
            JsonElement response = SendRequest(nameof(GetGroupList));
            var groups = new List<string>();
            
            if (response.TryGetProperty("groups", out var groupsElement) && groupsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var group in groupsElement.EnumerateArray())
                {
                    if (group.ValueKind == JsonValueKind.String)
                    {
                        groups.Add(group.GetString() ?? "");
                    }
                }
            }

            return groups;
        }

        /// <summary>
        /// Gets a Base64-encoded screenshot of a source.
        /// The imageWidth and imageHeight parameters are treated as "scale to inner", meaning the smallest ratio will be used and the aspect ratio of the original resolution is kept.
        /// If imageWidth and imageHeight are not specified, the compressed image will use the full resolution of the source.
        /// **Compatible with inputs and scenes.**
        /// </summary>
        /// <param name="sourceName">Name of the source to take a screenshot of</param>
        /// <param name="imageFormat">Image compression format to use. Use GetVersion to get compatible image formats</param>
        /// <param name="imageWidth">Width to scale the screenshot to</param>
        /// <param name="imageHeight">Height to scale the screenshot to</param>
        /// <param name="imageCompressionQuality">Compression quality to use. 0 for high compression, 100 for uncompressed. -1 to use "default"</param>
        /// <returns>Base64-encoded screenshot string</returns>
        public string GetSourceScreenshot(string sourceName, string imageFormat, int imageWidth = -1, int imageHeight = -1, int imageCompressionQuality = -1)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(sourceName), sourceName },
                { nameof(imageFormat), imageFormat }
            };

            if (imageWidth > 0)
                requestData[nameof(imageWidth)] = imageWidth;
            if (imageHeight > 0)
                requestData[nameof(imageHeight)] = imageHeight;
            if (imageCompressionQuality >= 0)
                requestData[nameof(imageCompressionQuality)] = imageCompressionQuality;

            JsonElement response = SendRequest(nameof(GetSourceScreenshot), JsonHelper.ToJsonElement(requestData));
            return JsonHelper.GetPropertyValue<string>(response, "imageData", "");
        }

        /// <summary>
        /// Opens the properties dialog of an input.
        /// </summary>
        /// <param name="inputName">Name of the input to open the dialog of</param>
        public void OpenInputPropertiesDialog(string inputName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName }
            };

            SendRequest(nameof(OpenInputPropertiesDialog), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Opens the filters dialog of an input.
        /// </summary>
        /// <param name="inputName">Name of the input to open the dialog of</param>
        public void OpenInputFiltersDialog(string inputName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName }
            };

            SendRequest(nameof(OpenInputFiltersDialog), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Opens the interact dialog of an input.
        /// </summary>
        /// <param name="inputName">Name of the input to open the dialog of</param>
        public void OpenInputInteractDialog(string inputName)
        {
            var requestData = new Dictionary<string, object>
            {
                { nameof(inputName), inputName }
            };

            SendRequest(nameof(OpenInputInteractDialog), JsonHelper.ToJsonElement(requestData));
        }

        /// <summary>
        /// Gets a list of connected monitors and information about them.
        /// </summary>
        /// <returns>a list of detected monitors with some information</returns>
        public List<Monitor> GetMonitorList()
        {
            JsonElement response = SendRequest(nameof(GetMonitorList));
            var monitors = new List<Monitor>();
            
            if (response.TryGetProperty("monitors", out var monitorsElement) && monitorsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var monitor in monitorsElement.EnumerateArray())
                {
                    var monitorObj = JsonHelper.Deserialize<Monitor>(monitor);
                    if (monitorObj != null)
                    {
                        monitors.Add(monitorObj);
                    }
                }
            }

            return monitors;
        }
    }
}
