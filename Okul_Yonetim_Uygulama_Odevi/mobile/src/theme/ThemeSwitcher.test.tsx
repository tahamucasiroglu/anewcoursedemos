import AsyncStorage from "@react-native-async-storage/async-storage";
import { render, screen, within } from "@testing-library/react-native";
import App from "../../App";
import tr from "../i18n/tr.json";

// Spec 0002, AC-3 ve AC-11: seçici başlık satırında, metinler tr.json'dan, ekran okuyucu grubu ve seçili olanı okur.
const fetchMock = jest.fn<Promise<Response>, [Request]>();
const originalFetch = globalThis.fetch;

beforeEach(async () => {
  await AsyncStorage.clear();
  fetchMock.mockReset();
  fetchMock.mockReturnValue(new Promise(() => {}));
  globalThis.fetch = fetchMock as unknown as typeof fetch;
});

afterAll(() => {
  globalThis.fetch = originalFetch;
});

// Grup kapsayıcısı bilinçli olarak "accessible" değil (iOS'ta içindeki seçenekler tek tek okunabilsin); bu yüzden
// etiketiyle bulunur ve rolü ayrıca doğrulanır.
async function findGroup() {
  const group = await screen.findByLabelText(tr["theme.label"]);
  expect(group.props.accessibilityRole).toBe("radiogroup");
  return group;
}

it("Tema grubunda Sistem, Açık, Koyu; yalnızca seçili olan checked", async () => {
  await render(<App />);
  const group = await findGroup();
  const radios = within(group).getAllByRole("radio");
  expect(radios.map((r) => r.props.accessibilityLabel)).toEqual([tr["theme.system"], tr["theme.light"], tr["theme.dark"]]);
  expect(radios.map((r) => r.props.accessibilityState?.checked)).toEqual([true, false, false]);
  expect(within(group).getByText(`☾ ${tr["theme.dark"]}`)).toBeTruthy();
});

it("seçici başlık satırında, ürün adının yanında", async () => {
  await render(<App />);
  const group = await findGroup();
  const header = screen.getByRole("header", { name: tr["app.title"] });
  expect(group.parent).toBe(header.parent);
});
