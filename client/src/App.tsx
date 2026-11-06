
import { useState } from "react";
import { toast, ToastContainer } from "react-toastify";
import 'react-toastify/dist/ReactToastify.css';

interface TranslationResponse {
    id: number;
    english: string;
    turkish: string;
    renglish: string;
}

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? (import.meta.env.DEV ? "http://localhost:5000" : "");

function ApiTest() {
    const [token, setToken] = useState("");
    const [username, setUsername] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [registerMode, setRegisterMode] = useState(false);
    const [accountError, setAccountError] = useState("");
    const [apiKey, setApiKey] = useState("");
    const [apiKeyConfigured, setApiKeyConfigured] = useState(false);
    const [accountLoading, setAccountLoading] = useState(false);
    const [topic, setTopic] = useState("Tourism");
    const [level, setLevel] = useState("A1");
    const [answer, setAnswer] = useState("");

    const [result, setResult] = useState<TranslationResponse | null>(null);

    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const authenticate = async (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setAccountLoading(true);
        setAccountError("");
        try {
            const response = await fetch(`${API_BASE}/api/Account/${registerMode ? "register" : "login"}`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(registerMode ? { username, email, password } : { email, password }),
            });
            const data = await response.json();
            if (!response.ok) throw new Error(data.error ?? "Hesap işlemi başarısız.");
            setToken(data.token);
            setUsername(data.username);
            setPassword("");
            const status = await fetch(`${API_BASE}/api/Account/api-key`, {
                headers: { Authorization: `Bearer ${data.token}` },
            });
            if (status.ok) setApiKeyConfigured((await status.json()).configured);
        } catch (err) {
            setAccountError(err instanceof Error ? err.message : "İstek tamamlanamadı.");
        } finally {
            setAccountLoading(false);
        }
    };

    const saveApiKey = async () => {
        setAccountError("");
        try {
            const response = await fetch(`${API_BASE}/api/Account/api-key`, {
                method: "PUT",
                headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
                body: JSON.stringify({ apiKey }),
            });
            if (!response.ok) throw new Error((await response.json()).error ?? "Anahtar kaydedilemedi.");
            setApiKey("");
            setApiKeyConfigured(true);
        } catch (err) {
            setAccountError(err instanceof Error ? err.message : "İstek tamamlanamadı.");
        }
    };

    const deleteApiKey = async () => {
        const response = await fetch(`${API_BASE}/api/Account/api-key`, {
            method: "DELETE",
            headers: { Authorization: `Bearer ${token}` },
        });
        if (response.ok) setApiKeyConfigured(false);
    };

    const logout = async () => {
        await fetch(`${API_BASE}/api/Account/logout`, {
            method: "POST",
            headers: { Authorization: `Bearer ${token}` },
        });
        setToken("");
        setApiKey("");
        setApiKeyConfigured(false);
    };

    if (!token) {
        return (
            <main style={{ maxWidth: 440, margin: "8vh auto", padding: 28, fontFamily: "Arial", color: "#263b36", background: "white", border: "1px solid #e8efeb", borderRadius: 16 }}>
                <h1>{registerMode ? "Hesap oluştur" : "Giriş yap"}</h1>
                <p>Groq anahtarınız hesabınıza şifreli olarak kaydedilir.</p>
                <form onSubmit={authenticate} style={{ display: "grid", gap: 12 }}>
                    {registerMode && <input required minLength={2} maxLength={50} placeholder="Kullanıcı adı" value={username} onChange={(e) => setUsername(e.target.value)} />}
                    <input required type="email" placeholder="E-posta" value={email} onChange={(e) => setEmail(e.target.value)} />
                    <input required type="password" minLength={registerMode ? 12 : 1} placeholder={registerMode ? "Parola (en az 12 karakter)" : "Parola"} value={password} onChange={(e) => setPassword(e.target.value)} />
                    {accountError && <p role="alert" style={{ color: "#bd554e" }}>{accountError}</p>}
                    <button disabled={accountLoading}>{accountLoading ? "İşleniyor…" : registerMode ? "Kayıt ol" : "Giriş yap"}</button>
                </form>
                <button style={{ marginTop: 12 }} onClick={() => { setRegisterMode(!registerMode); setAccountError(""); }}>
                    {registerMode ? "Zaten hesabım var" : "Yeni hesap oluştur"}
                </button>
            </main>
        );
    }

    // --------------------------------------------------
    // POST /api/Translation/generate
    // --------------------------------------------------
    const levelChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
        setLevel(event.target.value);
    };
    const topicChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
        setTopic(event.target.value);
    };
    const checkAnswer = async () => {
        setLoading(true);
        setError(null);
        try{
            const response = await fetch(`${API_BASE}/api/Translation/generateCheck`, 
                {
                    method: "POST",
                    headers: {"Content-Type": "application/json", Authorization: `Bearer ${token}`},
                    body: JSON.stringify({
                        "id": result?.id,
                        "english": answer
                    })
                }
            );
            if (response.ok) {
                toast.success('Başarılı !');
            }
            else{
                const errorText = await response.text();
                console.log(" Veri : ",answer);
                toast.error("Yanlış");
                throw new Error(
                    `HTTP ${response.status}: ${errorText}`
                );

            }

        }
        catch (err) {
            console.error("POST Error:", err);

            if (err instanceof Error) {
                setError(err.message);
            } else {
                setError("Bilinmeyen bir hata oluştu.");
            }
        } finally {
            setLoading(false);
        }
    };
    const generateSentence = async () => {
        if (!apiKeyConfigured) {
            setError("Önce Groq API anahtarınızı kaydedin.");
            return;
        }
        if (!topic.trim() || !level.trim()) {
            setError("Lütfen konu ve seviye seçin.");
            return;
        }

        setLoading(true);
        setError(null);
        setResult(null);

        try {
            const response = await fetch(
                `${API_BASE}/api/Translation/generate`,
                {
                    method: "POST",

                    headers: {
                        "Content-Type": "application/json",
                        Authorization: `Bearer ${token}`,
                    },

                    body: JSON.stringify({
                        Topic: topic.trim(),
                        Level: level.trim()
                    }),
                }
            );
            console.log(topic);
            console.log(level);
            // HTTP 400 / 500 / 429 vb.
            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(
                    `HTTP ${response.status}: ${errorText}`
                );
                
            }

            const data: TranslationResponse =
                await response.json();

            console.log("POST Response:", data);

            setResult(data);
        } catch (err) {
            console.error("POST Error:", err);

            if (err instanceof Error) {
                setError(err.message);
            } else {
                setError("Bilinmeyen bir hata oluştu.");
            }
        } finally {
            setLoading(false);
        }
    };

    return (
        <div
            style={{
                minHeight: "100vh",
                padding: "40px",
                backgroundColor: "#1e1e1e",
                color: "white",
                fontFamily: "Arial",
            }}
        >
            <div
                style={{
                    maxWidth: "800px",
                    margin: "0 auto",
                }}
            >
                <section style={{ marginBottom: 24, padding: 18, background: "#292929", borderRadius: 10 }}>
                    <div style={{ display: "flex", justifyContent: "space-between", gap: 12 }}>
                        <strong>Hesap: {username}</strong>
                        <button onClick={logout}>Çıkış</button>
                    </div>
                    <label htmlFor="groq-api-key">Groq API anahtarı</label>
                    <input id="groq-api-key" type="password" autoComplete="new-password" value={apiKey} onChange={(e) => setApiKey(e.target.value)} placeholder={apiKeyConfigured ? "Kayıtlı (değiştirmek için yeni anahtar girin)" : "gsk_…"} style={{ display: "block", width: "100%", margin: "8px 0", padding: 10 }} />
                    <button onClick={saveApiKey} disabled={!apiKey.trim()}>Şifreli kaydet</button>
                    {apiKeyConfigured && <button onClick={deleteApiKey} style={{ marginLeft: 8 }}>Anahtarı sil</button>}
                    <p style={{ margin: "8px 0 0", color: "#bbb", fontSize: 13 }}>{apiKeyConfigured ? "Anahtar bu hesaba kayıtlıdır." : "Anahtar henüz kaydedilmedi."}</p>
                    {accountError && <p role="alert" style={{ color: "#ff8a80" }}>{accountError}</p>}
                </section>
                <h1>Translation API Test</h1>

                <p>
                    Bu sayfa C# backend API'sini test etmek için
                    oluşturulmuştur.
                </p>

                <hr />

                {/* PROMPT */}

                <h2>POST Test</h2>

                <label>
                    Topic:
                </label>
                <br />
                <select id="topic-select" value={topic} onChange={topicChange} style={{
                        width: "100%",
                        marginTop: "10px",
                        padding: "10px",
                        fontSize: "16px",
                        boxSizing: "border-box",
                    }}>
                    <option value="Tourism">Tourism</option>
                    <option value="Daily routine">Daily routine</option>
                    <option value="Healing">Healing</option>
                    <option value="Requesting the Bill">Requesting the Bill</option>
                </select>

                <br />
                <label> Level :</label>
                <select id="level-select" value={level} onChange={levelChange} style={{
                        width: "100%",
                        marginTop: "10px",
                        padding: "10px",
                        fontSize: "16px",
                        boxSizing: "border-box",
                    }}>
                    <option value="A1">A1</option>
                    <option value="A2">A2</option>
                    <option value="B1">B1</option>
                    <option value="B2">B2</option>
                    <option value="C1">C1</option>
                    <option value="C2">C2</option>
                </select>

                <br />
                <br />

                <button
                    onClick={generateSentence}
                    disabled={loading}
                    style={{
                        padding: "12px 20px",
                        fontSize: "16px",
                        cursor: loading
                            ? "not-allowed"
                            : "pointer",
                    }}
                >
                    {loading
                        ? "İstek Gönderiliyor..."
                        : "POST - Yeni Cümle Oluştur"}
                </button>

                <br />
                {error && (
                    <p role="alert" style={{ color: "#ff8a80", marginTop: "12px" }}>
                        {error}
                    </p>
                )}
                {result && (
                    <div
                        style={{
                            marginTop: "30px",
                            padding: "20px",
                            backgroundColor: "#292929",
                            borderRadius: "10px",
                        }}
                    >
                        <h2>API Response</h2>

                        <div
                            style={{
                                marginTop: "20px",
                            }}
                        >
                        </div>

                        <div>
                            <strong>Shuffled English:</strong>
                            <p>
                                {result.renglish}
                            </p>
                            <strong>Turkish :</strong>
                            <p>{result.turkish}</p>
                        </div>

                    </div>
                )}
                <div>
                    <textarea 
                    value={answer}
                    onChange={(e) => setAnswer(e.target.value)}
                    rows={1}
                    style={{
                        width: "100%",
                        marginTop: "10px",
                        padding: "10px",
                        fontSize: "16px",
                        boxSizing: "border-box",
                    }}
                    ></textarea>
                </div>
                    <button
                    onClick={checkAnswer}
                    disabled={loading}
                    style={{
                        padding: "12px 20px",
                        fontSize: "16px",
                        cursor: loading
                            ? "not-allowed"
                            : "pointer",
                    }}
                    >
                    {loading
                        ? "İstek Gönderiliyor..."
                        : "Kontrol Et!"}
                    </button>
            </div>
            <ToastContainer></ToastContainer>
        </div>
    );
}

export default ApiTest;
