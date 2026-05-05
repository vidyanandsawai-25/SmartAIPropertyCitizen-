'use client';

import React, { useState, useEffect, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { 
  Languages, Search, ShieldCheck, Send, 
  FileText, CreditCard, Download, User, 
  ChevronRight, MapPin, Loader2
} from 'lucide-react';
import { i18n, Language } from '@/lib/i18n';
import { citizenApi } from '@/lib/api';
import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

type Step = 'lang' | 'search' | 'otp' | 'chat';

export default function SmartAIPage() {
  const [mounted, setMounted] = useState(false);
  const [step, setStep] = useState<Step>('lang');
  const [lang, setLang] = useState<Language>('mr');
  const [discounts, setDiscounts] = useState<any[]>([]);
  const [searchInput, setSearchInput] = useState('');
  const [searchResults, setSearchResults] = useState<any[]>([]);
  const [selectedProperty, setSelectedProperty] = useState<any>(null);
  const [otp, setOtp] = useState('');
  const [sessionId, setSessionId] = useState('');
  const [chatMessages, setChatMessages] = useState<{ role: 'user' | 'bot', text: string, data?: any, links?: any }[]>([]);
  const [chatInput, setChatInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const chatEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    setMounted(true);
    citizenApi.getDiscounts().then(setDiscounts).catch(() => {});
  }, []);

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [chatMessages]);

  // Auto-suggest search effect
  useEffect(() => {
    if (step === 'search' && searchInput.length >= 3) {
      const timer = setTimeout(() => {
        handleSearch();
      }, 500);
      return () => clearTimeout(timer);
    } else if (searchInput.length === 0) {
      setSearchResults([]);
    }
  }, [searchInput, step]);

  if (!mounted) return null;

  const handleLanguageSelect = (l: Language) => {
    setLang(l);
    setStep('search');
  };

  const handleSearch = async () => {
    if (!searchInput) return;
    setIsLoading(true);
    try {
      const results = await citizenApi.search(searchInput);
      setSearchResults(results);
    } catch (e) {
      console.error(e);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSelectProperty = async (prop: any) => {
    setSelectedProperty(prop);
    setIsLoading(true);
    try {
      const res = await citizenApi.sendOtp(prop.ownerId, prop.propertyNo);
      setSessionId(res.sessionId);
      setStep('otp');
    } catch (e: any) {
      alert("Error: " + e.message);
      console.error(e);
    } finally {
      setIsLoading(false);
    }
  };

  const handleVerifyOtp = async () => {
    if (otp.length < 6) return;
    setIsLoading(true);
    try {
      const res = await citizenApi.verifyOtp(sessionId, otp);
      if (res.isSuccess) {
        setStep('chat');
        setChatMessages([{ role: 'bot', text: i18n[lang].welcome }]);
      }
    } catch (e: any) {
      alert(e.message);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSendMessage = async (msg?: string) => {
    const text = msg || chatInput;
    if (!text) return;
    if (!msg) setChatInput('');
    
    setChatMessages(prev => [...prev, { role: 'user', text }]);
    setIsLoading(true);

    try {
      const res = await citizenApi.chat(text, sessionId, lang);
      setChatMessages(prev => [...prev, { 
        role: 'bot', 
        text: res.responseText, 
        data: res.data,
        links: { pay: res.paymentUrl, doc: res.downloadUrl }
      }]);
    } catch (e) {
      console.error(e);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col items-center justify-center p-0 sm:p-4 text-slate-900 overflow-hidden">
      <motion.div 
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        className="w-full max-w-lg bg-white h-[100dvh] sm:h-[85vh] sm:max-h-[900px] sm:rounded-[2.5rem] overflow-hidden shadow-[0_20px_50px_rgba(0,0,0,0.1)] flex flex-col border border-slate-100 relative"
      >
        
        {/* Header */}
        <header className="bg-white p-4 text-center relative shrink-0 border-b border-slate-50 z-10 shadow-[0_2px_10px_rgba(0,0,0,0.02)]">
          <div className="flex flex-col items-center gap-1.5">
            <div className="p-1 rounded-full shadow-sm bg-slate-50">
              <img 
                src="https://akolamc.in/images/councilLogo/akola.png" 
                alt="Logo" 
                className="w-12 h-12 object-contain"
              />
            </div>
            <h1 className="text-lg sm:text-xl font-bold leading-tight text-blue-900 mt-1">{i18n[lang].name}</h1>
            <p className="text-[9px] sm:text-[10px] text-blue-600/60 font-bold tracking-[0.2em] uppercase">SmartAI Assistant</p>
          </div>
          {step !== 'lang' && (
            <button 
              onClick={() => setStep('lang')} 
              className="absolute right-6 top-6 text-slate-400 hover:text-blue-600 transition-colors p-2 rounded-xl hover:bg-blue-50"
            >
              <Languages size={20} />
            </button>
          )}
        </header>

        {/* Content */}
        <main className="flex-1 overflow-hidden flex flex-col relative bg-slate-50/50">
          <AnimatePresence mode="wait">
            
            {/* Step: Language */}
            {step === 'lang' && (
              <motion.div 
                key="lang"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -20 }}
                className="h-full flex flex-col p-8 space-y-8"
              >
                {/* Discount Marquee */}
                {discounts && discounts.length > 0 && (
                  <div className="px-2">
                    <div className="bg-orange-50 border border-orange-200/50 text-orange-600 rounded-full px-5 py-2 text-sm font-semibold shadow-sm overflow-hidden whitespace-nowrap">
                      <div dangerouslySetInnerHTML={{ __html: `<marquee scrollamount="4">${discounts.map(d => `🔥 ${d.discountDiscription}: ${d.discountPercentage}% Discount`).join('  &nbsp;|&nbsp;  ')}</marquee>` }} />
                    </div>
                  </div>
                )}
                
                <div className="text-center pt-4">
                  <h2 className="text-2xl font-bold text-slate-800">निवडा / Select Language</h2>
                  <p className="text-slate-500 mt-2">Please select your preferred language</p>
                </div>

                <div className="grid grid-cols-1 gap-4">
                  {(['mr', 'hi', 'en'] as Language[]).map((l) => (
                    <button 
                      key={l}
                      onClick={() => handleLanguageSelect(l)}
                      className="group p-6 border border-slate-100 rounded-3xl hover:border-blue-500 hover:bg-blue-50 transition-all text-center relative overflow-hidden"
                    >
                      <span className="block text-xl font-bold text-slate-800 mb-1">
                        {l === 'mr' ? 'मराठी' : l === 'hi' ? 'हिंदी' : 'English'}
                      </span>
                      <span className="text-sm text-slate-400 font-medium uppercase tracking-wider">
                        {l === 'mr' ? 'Marathi' : l === 'hi' ? 'Hindi' : 'English'}
                      </span>
                      <ChevronRight className="absolute right-6 top-1/2 -translate-y-1/2 text-slate-200 group-hover:text-blue-500 transition-colors" />
                    </button>
                  ))}
                </div>
              </motion.div>
            )}

            {/* Step: Search */}
            {step === 'search' && (
              <motion.div 
                key="search"
                initial={{ opacity: 0, x: 50 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -50 }}
                className="h-full flex flex-col p-6 sm:p-8 space-y-6 overflow-hidden"
              >
                <div className="text-center shrink-0">
                  <h2 className="text-2xl font-bold text-slate-800">{i18n[lang].searchTitle}</h2>
                  <p className="text-sm text-slate-500 mt-2">{i18n[lang].searchDesc}</p>
                </div>

                <div className="relative group shrink-0">
                  <input 
                    type="text" 
                    value={searchInput}
                    onChange={(e) => setSearchInput(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                    placeholder={i18n[lang].searchPlaceholder}
                    className="w-full p-5 pl-14 rounded-3xl border border-slate-200 outline-none focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 transition-all bg-white text-lg shadow-sm"
                  />
                  <Search className="absolute left-5 top-1/2 -translate-y-1/2 text-slate-400 w-6 h-6 group-focus-within:text-blue-500 transition-colors" />
                </div>

                <button 
                  onClick={handleSearch}
                  disabled={isLoading}
                  className="w-full shrink-0 bg-blue-600 text-white p-5 rounded-3xl font-bold shadow-xl shadow-blue-200 hover:bg-blue-700 transition-all active:scale-[0.98] text-lg disabled:opacity-70 flex items-center justify-center gap-2"
                >
                  {isLoading ? <Loader2 className="animate-spin" /> : i18n[lang].searchBtn}
                </button>

                <div className="flex-1 overflow-y-auto space-y-3 pr-2 custom-scrollbar pb-4">
                  {searchResults.map((res, i) => (
                    <motion.div 
                      initial={{ opacity: 0, y: 10 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ delay: i * 0.05 }}
                      key={i}
                      onClick={() => handleSelectProperty(res)}
                      className="p-5 border border-slate-100 rounded-3xl cursor-pointer hover:bg-blue-50/50 hover:border-blue-200 transition-all bg-white shadow-sm flex items-center gap-4 group"
                    >
                      <div className="w-12 h-12 bg-slate-50 text-slate-400 rounded-2xl flex items-center justify-center group-hover:bg-white group-hover:text-blue-500 transition-colors shrink-0">
                        <MapPin size={24} />
                      </div>
                      <div className="flex-1 min-w-0">
                        <h4 className="font-bold text-slate-800 truncate">{res.ownerNameMarathi}</h4>
                        <div className="flex flex-wrap items-center gap-2 mt-1">
                          <p className="text-xs text-blue-600 font-bold bg-blue-50 border border-blue-100 px-2 py-1 rounded-md">{res.upicNo}</p>
                          <p className="text-xs text-slate-500 font-medium bg-slate-100 px-2 py-1 rounded-md">#{res.propertyNo}</p>
                          {res.mobileNo && (
                            <p className="text-xs text-slate-400 font-bold px-2 py-1">📞 {res.mobileNo}</p>
                          )}
                        </div>
                      </div>
                      <ChevronRight size={20} className="text-slate-200 group-hover:text-blue-500 transition-colors shrink-0" />
                    </motion.div>
                  ))}
                  {searchResults.length === 0 && searchInput && !isLoading && (
                    <p className="text-center text-slate-400 py-10 font-medium">{i18n[lang].noResults}</p>
                  )}
                </div>
              </motion.div>
            )}

            {/* Step: OTP */}
            {step === 'otp' && (
              <motion.div 
                key="otp"
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                className="space-y-8 flex-1 py-4 flex flex-col justify-center"
              >
                <div className="text-center">
                  <div className="w-24 h-24 bg-blue-50 text-blue-600 rounded-[2.5rem] flex items-center justify-center mx-auto mb-6 shadow-inner">
                    <ShieldCheck size={48} />
                  </div>
                  <h2 className="text-2xl font-bold text-slate-800">{i18n[lang].otpTitle}</h2>
                  <p className="text-sm text-slate-500 mt-2 px-10 leading-relaxed">
                    {i18n[lang].otpWait} <br/> 
                    <span className="font-bold text-blue-600 mt-1 inline-block">{selectedProperty?.mobileNo}</span>
                  </p>
                </div>

                <div className="px-4">
                  <input 
                    type="text" 
                    maxLength={6}
                    value={otp}
                    onChange={(e) => setOtp(e.target.value)}
                    placeholder="••••••"
                    className="w-full text-center text-4xl font-bold p-6 rounded-3xl border border-slate-200 outline-none focus:border-blue-500 focus:ring-8 focus:ring-blue-500/5 transition-all tracking-[0.4em] bg-white shadow-sm"
                  />
                </div>

                <button 
                  onClick={handleVerifyOtp}
                  disabled={isLoading || otp.length < 6}
                  className="w-full bg-blue-600 text-white p-5 rounded-3xl font-bold shadow-xl shadow-blue-200 hover:bg-blue-700 transition-all active:scale-[0.98] text-lg disabled:opacity-50 flex items-center justify-center gap-2"
                >
                  {isLoading ? <Loader2 className="animate-spin" /> : i18n[lang].otpVerify}
                </button>
              </motion.div>
            )}

            {/* Step: Chat */}
            {step === 'chat' && (
              <motion.div 
                key="chat"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                className="h-full flex flex-col w-full"
              >
                {/* Chat Header Info */}
                <div className="p-4 px-6 border-b border-slate-50 flex items-center gap-3 bg-white">
                  <div className="w-10 h-10 bg-blue-50 text-blue-600 rounded-xl flex items-center justify-center shadow-sm">
                    <User size={20} />
                  </div>
                  <div>
                    <h4 className="text-xs font-bold text-slate-800 truncate w-40">{selectedProperty?.ownerNameMarathi}</h4>
                    <p className="text-[10px] text-blue-600 font-bold uppercase tracking-wider">#{selectedProperty?.propertyNo}</p>
                  </div>
                </div>

                {/* Messages */}
                <div className="flex-1 overflow-y-auto p-6 space-y-6 custom-scrollbar bg-slate-50/30">
                  {chatMessages.map((msg, i) => (
                      <motion.div 
                        key={i}
                        initial={{ opacity: 0, y: 10, scale: 0.95 }}
                        animate={{ opacity: 1, y: 0, scale: 1 }}
                        className={cn(
                          "flex flex-col",
                          msg.data ? "w-full" : "max-w-[90%]",
                          msg.role === 'user' ? "ml-auto items-end" : "mr-auto items-start"
                        )}
                      >
                        <div className={cn(
                          "p-4 px-5 rounded-2xl shadow-sm border w-full",
                          msg.role === 'user' 
                            ? "bg-blue-600 text-white border-blue-500 rounded-br-none" 
                            : "bg-white text-slate-700 border-slate-100 rounded-bl-none"
                        )}>
                          <p className="text-sm font-medium leading-relaxed">{msg.text}</p>
                        
                          {/* Data Rendering (Tables) */}
                          {msg.data && Array.isArray(msg.data) && msg.data.length > 0 && (
                            <div className="mt-4 w-full overflow-hidden border border-slate-200 rounded-2xl shadow-sm bg-white">
                              <div className="w-full overflow-x-auto no-scrollbar">
                              {/* Demand Table */}
                              {msg.data[0].hasOwnProperty('headName') ? (
                                <table className="w-full text-[9px] sm:text-[10px] text-left border-collapse table-auto">
                                  <thead className="bg-slate-50 text-slate-500 font-bold border-b border-slate-100">
                                    <tr>
                                      <th className="px-3 py-2 whitespace-nowrap">{i18n[lang].thHead}</th>
                                      <th className="px-3 py-2 text-right whitespace-nowrap">{i18n[lang].thPrev}</th>
                                      <th className="px-3 py-2 text-right whitespace-nowrap">{i18n[lang].thCurr}</th>
                                      <th className="px-3 py-2 text-right whitespace-nowrap">{i18n[lang].thTotal}</th>
                                    </tr>
                                  </thead>
                                  <tbody className="divide-y divide-slate-50">
                                    {msg.data.map((row: any, idx: number) => (
                                      <tr key={idx} className="hover:bg-slate-50/50 transition-colors">
                                        <td className="px-3 py-2 font-bold text-slate-700 leading-tight">{row.headName}</td>
                                        <td className="px-3 py-2 text-right text-slate-500 font-medium">₹{row.previousBalance}</td>
                                        <td className="px-3 py-2 text-right text-slate-500 font-medium">₹{row.currentDemand}</td>
                                        <td className="px-3 py-2 text-right font-black text-blue-900">₹{row.totalAmount}</td>
                                      </tr>
                                    ))}
                                  </tbody>
                                </table>
                              ) : (
                                /* Receipts Table */
                                <table className="w-full text-[8px] sm:text-[9px] text-left border-collapse table-auto">
                                  <thead className="bg-slate-50 text-slate-500 font-bold border-b border-slate-100">
                                    <tr>
                                      <th className="px-2 py-2 whitespace-nowrap">{i18n[lang].thReceiptNo}</th>
                                      <th className="px-2 py-2 text-right whitespace-nowrap">{i18n[lang].thAmount}</th>
                                      <th className="px-2 py-2 whitespace-nowrap">{i18n[lang].thDate}</th>
                                      <th className="px-2 py-2 whitespace-nowrap">{i18n[lang].thResource}</th>
                                      <th className="px-2 py-2 whitespace-nowrap">{i18n[lang].thMode}</th>
                                    </tr>
                                  </thead>
                                  <tbody className="divide-y divide-slate-50">
                                    {msg.data.map((row: any, idx: number) => (
                                      <tr key={idx} className="hover:bg-slate-50/50 transition-colors">
                                        <td className="px-2 py-2 text-slate-600 font-bold leading-tight break-all">{row.receiptID}</td>
                                        <td className="px-2 py-2 text-right font-black text-blue-900">₹{row.amount}</td>
                                        <td className="px-2 py-2 text-slate-500 font-medium whitespace-nowrap">{row.transactionDate}</td>
                                        <td className="px-2 py-2 text-slate-500 font-medium leading-tight">{row.paymentResource}</td>
                                        <td className="px-2 py-2 text-slate-400 font-medium whitespace-nowrap">{row.paymentMode}</td>
                                      </tr>
                                    ))}
                                  </tbody>
                                </table>
                              )}
                            </div>
                          </div>
                        )}

                        {/* Property Details Data (3-Column Grid) */}
                        {msg.data && !Array.isArray(msg.data) && (
                          <div className="mt-4 bg-white rounded-2xl border border-slate-100 overflow-hidden shadow-md">
                            <div className="p-5 space-y-6">
                              {/* Row 1: UPIC, Property No, Description */}
                              <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                                <div className="flex flex-col gap-1">
                                  <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider">{i18n[lang].thUpic}</span>
                                  <span className="text-sm text-blue-700 font-black">{msg.data.upicNo}</span>
                                </div>
                                <div className="flex flex-col gap-1">
                                  <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider">{i18n[lang].thPropertyNo}</span>
                                  <span className="text-sm text-blue-900 font-black">{msg.data.propertyNo}</span>
                                </div>
                                <div className="flex flex-col gap-1">
                                  <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider">{i18n[lang].thDescription}</span>
                                  <span className="text-sm text-indigo-700 font-bold">{msg.data.propertyDescription}</span>
                                </div>
                              </div>

                              {/* Row 2: Owner, Old Prop, Mobile */}
                              <div className="grid grid-cols-1 md:grid-cols-3 gap-6 pt-4 border-t border-slate-50">
                                <div className="flex flex-col gap-1">
                                  <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider">{i18n[lang].thOwner}</span>
                                  <span className="text-sm text-slate-700 font-bold">{msg.data.ownerNameMarathi || msg.data.ownerNameEnglish}</span>
                                </div>
                                <div className="flex flex-col gap-1">
                                  <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider">{i18n[lang].thOldProperty}</span>
                                  <span className="text-sm text-slate-600 font-medium">{msg.data.oldPropertyNo || '-'}</span>
                                </div>
                                <div className="flex flex-col gap-1">
                                  <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider">{i18n[lang].thMobile}</span>
                                  <span className="text-sm text-slate-600 font-medium">{msg.data.mobileNo || '-'}</span>
                                </div>
                              </div>

                              {/* Row 3: Occupier, Building, Bill Date */}
                              <div className="grid grid-cols-1 md:grid-cols-3 gap-6 pt-4 border-t border-slate-50">
                                <div className="flex flex-col gap-1">
                                  <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider">{i18n[lang].thOccupier}</span>
                                  <span className="text-sm text-slate-600 font-medium">{msg.data.occupierNameMarathi || '-'}</span>
                                </div>
                                <div className="flex flex-col gap-1">
                                  <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider">{i18n[lang].thBuilding}</span>
                                  <span className="text-sm text-slate-600 font-medium">{msg.data.marathiOwnerDukanImarateNav || '-'}</span>
                                </div>
                                <div className="flex flex-col gap-1">
                                  <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider">{i18n[lang].thBillDate}</span>
                                  <span className="text-sm text-slate-600 font-medium">{msg.data.billDistributionDate || '-'}</span>
                                </div>
                              </div>

                              {/* Row 4: Society, Plot, Flat */}
                              <div className="grid grid-cols-1 md:grid-cols-3 gap-6 pt-4 border-t border-slate-50">
                                <div className="flex flex-col gap-1">
                                  <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider">{i18n[lang].thSociety}</span>
                                  <span className="text-sm text-slate-600 font-medium">{msg.data.marathiSocietyName || '-'}</span>
                                </div>
                                <div className="flex flex-col gap-1">
                                  <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider">{i18n[lang].thPlot}</span>
                                  <span className="text-sm text-slate-600 font-medium">{msg.data.newPlotNo || '-'}</span>
                                </div>
                                <div className="flex flex-col gap-1">
                                  <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider">{i18n[lang].thFlat}</span>
                                  <span className="text-sm text-slate-600 font-medium">{msg.data.flatOrShopNo || '-'}</span>
                                </div>
                              </div>

                              {/* Row 5: Address */}
                              <div className="pt-4 border-t border-slate-50">
                                <div className="flex flex-col gap-1">
                                  <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider">{i18n[lang].thAddress}</span>
                                  <span className="text-sm text-slate-600 font-medium leading-relaxed">{msg.data.marathiOwnerPatta || '-'}</span>
                                </div>
                              </div>
                            </div>
                          </div>
                        )}
                      </div>


                      {/* Links */}
                      {msg.links && (msg.links.pay || msg.links.doc) && (
                        <div className="flex flex-wrap gap-2 mt-3">
                          {msg.links.pay && (
                            <a href={msg.links.pay} target="_blank" className="flex items-center gap-2 bg-green-500 text-white p-3 px-5 rounded-xl text-[10px] font-black uppercase tracking-wider hover:bg-green-600 transition-all shadow-md active:scale-95">
                              <CreditCard size={12} /> {i18n[lang].onlinePayment}
                            </a>
                          )}
                          {msg.links.doc && (
                            <a href={msg.links.doc} target="_blank" className="flex items-center gap-2 bg-blue-500 text-white p-3 px-5 rounded-xl text-[10px] font-black uppercase tracking-wider hover:bg-blue-600 transition-all shadow-md active:scale-95">
                              <Download size={12} /> {i18n[lang].downloadNotice}
                            </a>
                          )}
                        </div>
                      )}
                    </motion.div>
                  ))}
                  <div ref={chatEndRef} />
                </div>

                {/* Quick Buttons */}
                <div className="p-4 px-6 space-y-4 border-t border-slate-50 bg-white">
                  <div className="flex flex-col gap-2">
                    {/* Full width primary button */}
                    <div className="grid grid-cols-2 gap-2">
                      <button 
                        onClick={() => handleSendMessage(i18n[lang].showDemand)}
                        className="w-full flex justify-center items-center gap-2 bg-blue-50 text-blue-700 px-4 py-3 rounded-2xl text-[11px] font-black border border-blue-100 hover:bg-blue-100 transition-colors uppercase tracking-wider shadow-sm"
                      >
                        <FileText size={14} /> {i18n[lang].showDemand}
                      </button>
                      
                      <button 
                        onClick={() => handleSendMessage(i18n[lang].showDetails)}
                        className="w-full flex justify-center items-center gap-2 bg-indigo-50 text-indigo-700 px-4 py-3 rounded-2xl text-[11px] font-black border border-indigo-100 hover:bg-indigo-100 transition-colors uppercase tracking-wider shadow-sm"
                      >
                        <MapPin size={14} /> {i18n[lang].showDetails}
                      </button>
                    </div>

                    
                    {/* Grid for secondary buttons */}
                    <div className="grid grid-cols-3 gap-2">
                      <button 
                        onClick={() => handleSendMessage(i18n[lang].showReceipts)}
                        className="flex flex-col justify-center items-center gap-1 bg-slate-50 text-slate-600 p-2 rounded-xl text-[9px] sm:text-[10px] font-black border border-slate-200 hover:bg-slate-100 transition-colors uppercase tracking-wider shadow-sm text-center leading-tight"
                      >
                        <FileText size={14} /> <span>{i18n[lang].showReceipts}</span>
                      </button>
                      <button 
                        onClick={() => handleSendMessage(i18n[lang].onlinePayment)}
                        className="flex flex-col justify-center items-center gap-1 bg-green-50 text-green-700 p-2 rounded-xl text-[9px] sm:text-[10px] font-black border border-green-200 hover:bg-green-100 transition-colors uppercase tracking-wider shadow-sm text-center leading-tight"
                      >
                        <CreditCard size={14} /> <span>{i18n[lang].onlinePayment}</span>
                      </button>
                      <button 
                        onClick={() => handleSendMessage(i18n[lang].downloadNotice)}
                        className="flex flex-col justify-center items-center gap-1 bg-orange-50 text-orange-700 p-2 rounded-xl text-[9px] sm:text-[10px] font-black border border-orange-200 hover:bg-orange-100 transition-colors uppercase tracking-wider shadow-sm text-center leading-tight"
                      >
                        <Download size={14} /> <span>{i18n[lang].downloadNotice}</span>
                      </button>
                    </div>
                  </div>

                  <div className="relative group flex items-center gap-2">
                    <input 
                      type="text" 
                      value={chatInput}
                      onChange={(e) => setChatInput(e.target.value)}
                      onKeyDown={(e) => e.key === 'Enter' && handleSendMessage()}
                      placeholder={i18n[lang].chatPlaceholder}
                      className="flex-1 p-4 pr-14 rounded-2xl border border-slate-200 outline-none focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 transition-all bg-white shadow-sm text-sm"
                    />
                    <button 
                      onClick={() => handleSendMessage()}
                      disabled={isLoading}
                      className="absolute right-2 top-1/2 -translate-y-1/2 p-2.5 bg-blue-600 text-white rounded-xl shadow-lg hover:bg-blue-700 transition-all active:scale-90 disabled:opacity-50"
                    >
                      {isLoading ? <Loader2 size={18} className="animate-spin" /> : <Send size={18} />}
                    </button>
                  </div>
                </div>
              </motion.div>
            )}

          </AnimatePresence>
        </main>

        {/* Footer */}
        <footer className="p-4 px-8 bg-white border-t border-slate-50 text-center shrink-0">
          <p className="text-[8px] text-slate-300 font-bold uppercase tracking-[0.2em] leading-relaxed">
            Design & Developed by <br/> 
            <span className="text-blue-500/60">Sthapatya Consultants(I) Pvt.Ltd, Amravati, Pune</span>
          </p>
        </footer>
      </motion.div>
    </div>
  );
}
