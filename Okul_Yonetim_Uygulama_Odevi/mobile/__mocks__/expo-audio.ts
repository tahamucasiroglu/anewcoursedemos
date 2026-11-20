// expo-audio'nun Jest sahtesi (spec 0012): gerçek mikrofon yok. Testler `__audio` ile izni, kaydın dosya adresini ve
// hazırlık hatasını ayarlar; kaç kez kayda başlanıp durdurulduğunu okur. Jest bu klasördeki sahteyi kendiliğinden kullanır.
export const __audio = {
  permission: true,
  uri: "file:///kayitlar/sesli-not.m4a" as string | null,
  failPrepare: false,
  recorded: 0,
  stopped: 0,
  reset() {
    this.permission = true;
    this.uri = "file:///kayitlar/sesli-not.m4a";
    this.failPrepare = false;
    this.recorded = 0;
    this.stopped = 0;
    recorder.uri = null;
  },
};

const recorder = {
  uri: null as string | null,
  async prepareToRecordAsync() {
    if (__audio.failPrepare) {
      throw new Error("mikrofon meşgul");
    }
  },
  record() {
    __audio.recorded += 1;
    recorder.uri = null;
  },
  async stop() {
    __audio.stopped += 1;
    recorder.uri = __audio.uri;
  },
};

export const RecordingPresets = { HIGH_QUALITY: { extension: ".m4a" } };

export function useAudioRecorder() {
  return recorder;
}

export async function requestRecordingPermissionsAsync() {
  return { granted: __audio.permission, status: __audio.permission ? "granted" : "denied", canAskAgain: true, expires: "never" };
}

export async function setAudioModeAsync() {}
