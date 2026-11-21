// expo-audio'nun Jest sahtesi (spec 0012): gerçek mikrofon yok. Testler `__audio` ile izni, kaydın dosya adresini ve
// hazırlık hatasını ayarlar; kaç kez kayda başlanıp durdurulduğunu okur. Kaydın süresi (`getStatus().durationMillis`)
// kayda başlandığından beri geçen saattir (sahte zamanlayıcıda sahte saat); `durationMillis` verilirse o döner.
// Jest bu klasördeki sahteyi kendiliğinden kullanır.
export const __audio = {
  permission: true,
  uri: "file:///kayitlar/sesli-not.m4a" as string | null,
  failPrepare: false,
  recorded: 0,
  stopped: 0,
  durationMillis: null as number | null,
  reset() {
    this.permission = true;
    this.uri = "file:///kayitlar/sesli-not.m4a";
    this.failPrepare = false;
    this.recorded = 0;
    this.stopped = 0;
    this.durationMillis = null;
    recorder.uri = null;
    recorder.isRecording = false;
  },
};

const recorder = {
  uri: null as string | null,
  isRecording: false,
  startedAt: 0,
  async prepareToRecordAsync() {
    if (__audio.failPrepare) {
      throw new Error("mikrofon meşgul");
    }
  },
  record() {
    __audio.recorded += 1;
    recorder.uri = null;
    recorder.isRecording = true;
    recorder.startedAt = Date.now();
  },
  async stop() {
    __audio.stopped += 1;
    recorder.uri = __audio.uri;
    recorder.isRecording = false;
  },
  getStatus() {
    const durationMillis = __audio.durationMillis ?? (recorder.isRecording ? Date.now() - recorder.startedAt : 0);
    return { canRecord: true, isRecording: recorder.isRecording, durationMillis, mediaServicesDidReset: false, url: recorder.uri };
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
