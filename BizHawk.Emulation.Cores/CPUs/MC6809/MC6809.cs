using System;

using BizHawk.Common;

// Motorola Corp 6809
namespace BizHawk.Emulation.Common.Components.MC6809
{
	public sealed partial class MC6809
	{
		// operations that can take place in an instruction
		public const ushort IDLE = 0; 
		public const ushort OP = 1;
		public const ushort RD = 2;
		public const ushort WR = 3;
		public const ushort TR = 4;
		public const ushort ADD16BR = 5;
		public const ushort ADD8 = 6;
		public const ushort SUB8 = 7;
		public const ushort ADC8 = 8;
		public const ushort SBC8 = 9;
		public const ushort INC16 = 10;
		public const ushort INC8 = 11;
		public const ushort DEC16 = 12;
		public const ushort DEC8 = 13;
		public const ushort ROL = 14;
		public const ushort ROR = 15;
		public const ushort COM = 16;
		public const ushort DA = 17;
		public const ushort AND8 = 18;
		public const ushort XOR8 = 19;
		public const ushort OR8 = 20;
		public const ushort CP8 = 21;
		public const ushort ASL = 22;
		public const ushort ASR = 23;
		public const ushort LSR = 24;
		public const ushort BIT = 25;
		public const ushort CWAI = 26;
		public const ushort SYNC = 27;
		public const ushort INT_GET = 28;
		public const ushort HALT_CHK = 29;
		public const ushort RD_INC = 30;
		public const ushort SET_ADDR = 31;
		public const ushort NEG = 32;
		public const ushort TST = 33;
		public const ushort CLR = 34;
		public const ushort OP_PG_2 = 35;
		public const ushort OP_PG_3 = 36;
		public const ushort SEX = 37;
		public const ushort RD_INC_OP = 38;
		public const ushort EXG = 39;
		public const ushort TFR = 40;
		public const ushort WR_DEC_LO = 41;
		public const ushort WR_DEC_HI = 42;
		public const ushort WR_HI = 43;
		public const ushort ADD8BR = 44;
		public const ushort ABX = 45;
		public const ushort MUL = 46;
		public const ushort JPE = 47;
		public const ushort IDX_DCDE = 48;
		public const ushort IDX_OP_BLD = 49;
		public const ushort EA_8 = 50;
		public const ushort EA_16 = 51;
		public const ushort PSH_n = 52;
		public const ushort PUL_n = 53;
		public const ushort WR_DEC_LO_OP = 54;
		public const ushort WR_DEC_HI_OP = 55;
		public const ushort SET_ADDR_PUL = 56;
		public const ushort SET_F_I = 57;
		public const ushort SET_E = 58;
		public const ushort ANDCC = 59;
		public const ushort CMP8 = 60;
		public const ushort SUB16 = 61;
		public const ushort ADD16 = 62;
		public const ushort CMP16 = 63;
		public const ushort CMP16D = 64;
		public const ushort WR_HI_INC = 65;

		public MC6809()
		{
			Reset();
		}

		public void Reset()
		{
			ResetRegisters();
			ResetInterrupts();
			TotalExecutedCycles = 0;
			Regs[PC] = 0xFFFE;
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							RD_INC, ALU, PC,
							RD_INC, ALU2, PC,
							SET_ADDR, PC, ALU, ALU2);

			IRQS = 6;
			instr_pntr = irq_pntr = 0;
		}

		// Memory Access 

		public Func<ushort, byte> ReadMemory;
		public Action<ushort, byte> WriteMemory;
		public Func<ushort, byte> PeekMemory;
		public Func<ushort, byte> DummyReadMemory;

		// Special Function for Speed switching executed on a STOP
		public Func<int, int> SpeedFunc;

		//this only calls when the first byte of an instruction is fetched.
		public Action<ushort> OnExecFetch;

		public void UnregisterMemoryMapper()
		{
			ReadMemory = null;
			ReadMemory = null;
			PeekMemory = null;
			DummyReadMemory = null;
		}

		public void SetCallbacks
		(
			Func<ushort, byte> ReadMemory,
			Func<ushort, byte> DummyReadMemory,
			Func<ushort, byte> PeekMemory,
			Action<ushort, byte> WriteMemory
		)
		{
			this.ReadMemory = ReadMemory;
			this.DummyReadMemory = DummyReadMemory;
			this.PeekMemory = PeekMemory;
			this.WriteMemory = WriteMemory;
		}

		//a little CDL related stuff
		public delegate void DoCDLCallbackType(ushort addr, MC6809.eCDLogMemFlags flags);

		public DoCDLCallbackType CDLCallback;

		public enum eCDLogMemFlags
		{
			FetchFirst = 1,
			FetchOperand = 2,
			Data = 4,
			Write = 8
		};

		// Execute instructions
		public void ExecuteOne()
		{
			//Console.Write(opcode_see + " ");
			switch (cur_instr[instr_pntr++])
			{
				case IDLE:
					// do nothing
					break;
				case OP:
					// Read the opcode of the next instruction					
					if (OnExecFetch != null) OnExecFetch(PC);
					if (TraceCallback != null) TraceCallback(State());
					if (CDLCallback != null) CDLCallback(PC, eCDLogMemFlags.FetchFirst);
					FetchInstruction(ReadMemory(Regs[PC]++));
					instr_pntr = 0;
					irq_pntr = -1;
					break;
				case OP_PG_2:
					FetchInstruction2(ReadMemory(Regs[PC]++));
					instr_pntr = 0;
					irq_pntr = -1;
					break;
				case OP_PG_3:
					FetchInstruction3(ReadMemory(Regs[PC]++));
					instr_pntr = 0;
					irq_pntr = -1;
					break;
				case RD:
					Read_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case RD_INC:
					Read_Inc_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case RD_INC_OP:
					Read_Inc_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					switch (cur_instr[instr_pntr++])
					{
						case AND8:
							AND8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case ADD8:
							ADD8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case ADC8:
							ADC8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case OR8:
							OR8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case XOR8:
							XOR8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case BIT:
							BIT_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case SUB8:
							SUB8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case SBC8:
							SBC8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case CMP8:
							CMP8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case DEC16:
							DEC16_Func(cur_instr[instr_pntr++]);
							break;
						case ADD8BR:
							ADD8BR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case TR:
							TR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case SET_ADDR:
							reg_d_ad = cur_instr[instr_pntr++];
							reg_h_ad = cur_instr[instr_pntr++];
							reg_l_ad = cur_instr[instr_pntr++];

							Regs[reg_d_ad] = (ushort)((Regs[reg_h_ad] << 8) | Regs[reg_l_ad]);
							break;
						case JPE:
							if (!FlagE) { instr_pntr = 45; };
							break;
						case IDX_DCDE:
							Index_decode();
							break;
						case IDX_OP_BLD:
							Index_Op_Builder();
							break;
						case EA_8:
							Regs[IDX_EA] = (ushort)(Regs[indexed_reg] + (((Regs[ALU2] & 0x80) == 0x80) ? (Regs[ALU2] | 0xFF00) : Regs[ALU2]));
							Index_Op_Builder();
							break;
						case PUL_n:
							PUL_n_BLD(cur_instr[instr_pntr++]);
							break;
						case SET_ADDR_PUL:
							Regs[cur_instr[instr_pntr++]] = (ushort)((Regs[ALU2] << 8) + Regs[ADDR]);
							PUL_n_BLD(cur_instr[instr_pntr++]);
							break;
					}
					break;
				case WR:
					Write_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case WR_DEC_LO:
					Write_Dec_Lo_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case WR_DEC_HI:
					Write_Dec_HI_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case WR_DEC_LO_OP:
					Write_Dec_Lo_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					switch (cur_instr[instr_pntr++])
					{
						case PSH_n:
							PSH_n_BLD(cur_instr[instr_pntr++]);
							break;
					}
					break;
				case WR_DEC_HI_OP:
					Write_Dec_HI_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					switch (cur_instr[instr_pntr++])
					{
						case PSH_n:
							PSH_n_BLD(cur_instr[instr_pntr++]);
							break;
					}
					break;
				case WR_HI:
					Write_Hi_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case WR_HI_INC:
					Write_Hi_Inc_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case TR:
					TR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case EXG:
					EXG_Func(cur_instr[instr_pntr++]);
					break;
				case IDX_OP_BLD:
					Index_Op_Builder();
					break;
				case EA_16:
					Regs[IDX_EA] = (ushort)(Regs[indexed_reg] + Regs[ADDR]);
					Index_Op_Builder();
					break;
				case TFR:
					TFR_Func(cur_instr[instr_pntr++]);
					break;
				case SET_ADDR:
					reg_d_ad = cur_instr[instr_pntr++];
					reg_h_ad = cur_instr[instr_pntr++];
					reg_l_ad = cur_instr[instr_pntr++];

					// Console.WriteLine(reg_d_ad + " " + reg_h_ad + " " + reg_l_ad);
					// Console.WriteLine(Regs[reg_d_ad] + " " + Regs[reg_h_ad] + " " + Regs[reg_l_ad]);

					Regs[reg_d_ad] = (ushort)((Regs[reg_h_ad] << 8) | Regs[reg_l_ad]);
					break;
				case NEG:
					NEG_8_Func(cur_instr[instr_pntr++]);
					break;
				case TST:
					TST_Func(cur_instr[instr_pntr++]);
					break;
				case CLR:
					CLR_Func(cur_instr[instr_pntr++]);
					break;
				case SEX:
					SEX_Func(cur_instr[instr_pntr++]);
					break;
				case ABX:
					Regs[X] += Regs[B];
					break;
				case MUL:
					Mul_Func();
					break;
				case SET_F_I:
					FlagI = true; FlagF = true;
					break;
				case SET_E:
					FlagE = true;
					break;
				case ANDCC:
					Regs[CC] &= Regs[instr_pntr++];
					break;
				case PSH_n:
					PSH_n_BLD(cur_instr[instr_pntr++]);
					break;
				case PUL_n:
					PUL_n_BLD(cur_instr[instr_pntr++]);
					break;
				case ADD16BR:
					ADD16BR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case ADD8BR:
					ADD8BR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case ADD8:
					ADD8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case SUB8:
					SUB8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case ADC8:
					ADC8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case SBC8:
					SBC8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case CMP8:
					CMP8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case INC16:
					INC16_Func(cur_instr[instr_pntr++]);
					break;
				case INC8:
					INC8_Func(cur_instr[instr_pntr++]);
					break;
				case DEC16:
					DEC16_Func(cur_instr[instr_pntr++]);
					break;
				case SUB16:
					SUB16_Func(cur_instr[instr_pntr++]);
					break;
				case ADD16:
					ADD16_Func(cur_instr[instr_pntr++]);
					break;
				case CMP16:
					CMP16_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case CMP16D:
					CMP16D_Func(cur_instr[instr_pntr++]);
					break;
				case DEC8:
					DEC8_Func(cur_instr[instr_pntr++]);
					break;
				case ROL:
					ROL_Func(cur_instr[instr_pntr++]);
					break;
				case ROR:
					ROR_Func(cur_instr[instr_pntr++]);
					break;
				case COM:
					COM_Func(cur_instr[instr_pntr++]);
					break;
				case DA:
					DA_Func(cur_instr[instr_pntr++]);
					break;
				case AND8:
					AND8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case XOR8:
					XOR8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case OR8:
					OR8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case CP8:
					CP8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case ASL:
					ASL_Func(cur_instr[instr_pntr++]);
					break;
				case ASR:
					ASR_Func(cur_instr[instr_pntr++]);
					break;
				case LSR:
					LSR_Func(cur_instr[instr_pntr++]);
					break;
				case BIT:
					BIT_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case CWAI:
					if (NMIPending)
					{
						Regs[ADDR] = 0xFFFC;
						PopulateCURINSTR(RD_INC, ALU, ADDR,
										RD_INC, ALU2, ADDR,
										SET_ADDR, PC, ALU, ALU2);
						irq_pntr = -1;
						IRQS = 3;

						if (TraceCallback != null) { TraceCallback(new TraceInfo { Disassembly = "====CWAI NMI====", RegisterInfo = "" }); }
					}
					else if (FIRQPending && !FlagF)
					{
						Regs[ADDR] = 0xFFF6;
						PopulateCURINSTR(RD_INC, ALU, ADDR,
										RD_INC, ALU2, ADDR,
										SET_ADDR, PC, ALU, ALU2);
						irq_pntr = -1;
						IRQS = 3;

						if (TraceCallback != null) { TraceCallback(new TraceInfo { Disassembly = "====CWAI FIRQ====", RegisterInfo = "" }); }
					}
					else if (IRQPending && !FlagI)
					{
						Regs[ADDR] = 0xFFF8;
						PopulateCURINSTR(RD_INC, ALU, ADDR,
										RD_INC, ALU2, ADDR,
										SET_ADDR, PC, ALU, ALU2);
						irq_pntr = -1;
						IRQS = 3;

						if (TraceCallback != null) { TraceCallback(new TraceInfo { Disassembly = "====CWAI IRQ====", RegisterInfo = "" }); }
					}
					else
					{
						PopulateCURINSTR(CWAI);
						irq_pntr = 0;
						IRQS = -1;
					}
					instr_pntr = 0;
					break;
				case SYNC:
					IN_SYNC = true;
					IRQS = 1;				
					instr_pntr = irq_pntr = 0;
					PopulateCURINSTR(SYNC);
					break;
				case INT_GET:

					break;
				case HALT_CHK:

					break;
			}

			if (++irq_pntr == IRQS)
			{
				// NMI has priority
				if (NMIPending)
				{
					NMIPending = false;

					if (TraceCallback != null) { TraceCallback(new TraceInfo { Disassembly = "====NMI====", RegisterInfo = "" }); }

					IN_SYNC = false;
					NMI_();
					NMICallback();
					instr_pntr = irq_pntr = 0;
				}
				// fast IRQ has next priority
				else if (FIRQPending)
				{
					if (!FlagF)
					{
						FIRQPending = false;

						if (TraceCallback != null) { TraceCallback(new TraceInfo { Disassembly = "====FIRQ====", RegisterInfo = "" }); }

						IN_SYNC = false;
						FIRQ_();
						FIRQCallback();
						instr_pntr = irq_pntr = 0;
					}
					else if (IN_SYNC)
					{
						FIRQPending = false;

						if (TraceCallback != null) { TraceCallback(new TraceInfo { Disassembly = "====SYNC====", RegisterInfo = "" }); }

						IN_SYNC = false;
						IRQS = 1;
						instr_pntr = irq_pntr = 0;
						PopulateCURINSTR(IDLE);
					}
				}
				// then regular IRQ
				else if (IRQPending && !FlagI)
				{
					if (!FlagI)
					{
						IRQPending = false;

						if (TraceCallback != null) { TraceCallback(new TraceInfo { Disassembly = "====IRQ====", RegisterInfo = "" }); }

						IN_SYNC = false;
						IRQ_();
						IRQCallback();
						instr_pntr = irq_pntr = 0;
					}
					else if (IN_SYNC)
					{
						IRQPending = false;

						if (TraceCallback != null) { TraceCallback(new TraceInfo { Disassembly = "====SYNC====", RegisterInfo = "" }); }

						IN_SYNC = false;
						IRQS = 1;
						instr_pntr = irq_pntr = 0;
						PopulateCURINSTR(IDLE);
					}
				}
				// otherwise start the next instruction
				else
				{
					PopulateCURINSTR(OP);
					instr_pntr = irq_pntr = 0;
					IRQS = -1;
				}
			}

			TotalExecutedCycles++;
		}

		// tracer stuff

		public Action<TraceInfo> TraceCallback;

		public string TraceHeader
		{
			get { return "MC6809: PC, machine code, mnemonic, operands, registers (A, B, X, Y, US, SP, DP, CC), Cy, flags (EFHINZVC)"; }
		}

		public TraceInfo State(bool disassemble = true)
		{
			ushort notused;

			return new TraceInfo
			{
				Disassembly = string.Format(
					"{0} ",
					disassemble ? Disassemble(Regs[PC], ReadMemory, out notused) : "---").PadRight(50),
				RegisterInfo = string.Format(
					"A:{0:X2} B:{1:X2} X:{2:X4} Y:{3:X4} US:{4:X4} SP:{5:X4} DP:{6:X2} CC:{7:X2} Cy:{8} {9}{10}{11}{12}{13}{14}{15}{16}",
					Regs[A],
					Regs[B],
					Regs[X],
					Regs[Y],
					Regs[US],
					Regs[SP],
					Regs[DP],
					Regs[CC],
					TotalExecutedCycles,
					FlagE ? "E" : "e",
					FlagF ? "F" : "f",
					FlagH ? "H" : "h",
					FlagI ? "I" : "i",
					FlagN ? "N" : "n",
					FlagZ ? "Z" : "z",
					FlagV ? "V" : "v",
					FlagC ? "C" : "c"			
					)
			};
		}

		/// <summary>
		/// Optimization method to set cur_instr
		/// </summary>	
		private void PopulateCURINSTR(ushort d0 = 0, ushort d1 = 0, ushort d2 = 0, ushort d3 = 0, ushort d4 = 0, ushort d5 = 0, ushort d6 = 0, ushort d7 = 0, ushort d8 = 0,
			ushort d9 = 0, ushort d10 = 0, ushort d11 = 0, ushort d12 = 0, ushort d13 = 0, ushort d14 = 0, ushort d15 = 0, ushort d16 = 0, ushort d17 = 0, ushort d18 = 0,
			ushort d19 = 0, ushort d20 = 0, ushort d21 = 0, ushort d22 = 0, ushort d23 = 0, ushort d24 = 0, ushort d25 = 0, ushort d26 = 0, ushort d27 = 0, ushort d28 = 0,
			ushort d29 = 0, ushort d30 = 0, ushort d31 = 0, ushort d32 = 0, ushort d33 = 0, ushort d34 = 0, ushort d35 = 0, ushort d36 = 0, ushort d37 = 0, ushort d38 = 0,
			ushort d39 = 0, ushort d40 = 0, ushort d41 = 0, ushort d42 = 0, ushort d43 = 0, ushort d44 = 0, ushort d45 = 0, ushort d46 = 0, ushort d47 = 0, ushort d48 = 0,
			ushort d49 = 0, ushort d50 = 0, ushort d51 = 0, ushort d52 = 0, ushort d53 = 0, ushort d54 = 0, ushort d55 = 0, ushort d56 = 0, ushort d57 = 0, ushort d58 = 0)
		{
			cur_instr[0] = d0; cur_instr[1] = d1; cur_instr[2] = d2;
			cur_instr[3] = d3; cur_instr[4] = d4; cur_instr[5] = d5;
			cur_instr[6] = d6; cur_instr[7] = d7; cur_instr[8] = d8;
			cur_instr[9] = d9; cur_instr[10] = d10; cur_instr[11] = d11;
			cur_instr[12] = d12; cur_instr[13] = d13; cur_instr[14] = d14;
			cur_instr[15] = d15; cur_instr[16] = d16; cur_instr[17] = d17;
			cur_instr[18] = d18; cur_instr[19] = d19; cur_instr[20] = d20;
			cur_instr[21] = d21; cur_instr[22] = d22; cur_instr[23] = d23;
			cur_instr[24] = d24; cur_instr[25] = d25; cur_instr[26] = d26;
			cur_instr[27] = d27; cur_instr[28] = d28; cur_instr[29] = d29;
			cur_instr[30] = d30; cur_instr[31] = d31; cur_instr[32] = d32;
			cur_instr[33] = d33; cur_instr[34] = d34; cur_instr[35] = d35;
			cur_instr[36] = d36; cur_instr[37] = d37; cur_instr[38] = d38;
			cur_instr[39] = d39; cur_instr[40] = d40; cur_instr[41] = d41;
			cur_instr[42] = d42; cur_instr[43] = d43; cur_instr[44] = d44;
			cur_instr[45] = d45; cur_instr[46] = d46; cur_instr[47] = d47;
			cur_instr[48] = d48; cur_instr[49] = d49; cur_instr[50] = d50;
			cur_instr[51] = d51; cur_instr[52] = d52; cur_instr[53] = d53;
			cur_instr[54] = d54; cur_instr[55] = d55; cur_instr[56] = d56;
			cur_instr[57] = d57; cur_instr[58] = d58;
		}

		// State Save/Load
		public void SyncState(Serializer ser)
		{
			ser.BeginSection("MC6809");

			ser.Sync("IN_SYNC", ref IN_SYNC);
			ser.Sync("NMIPending", ref NMIPending);
			ser.Sync("FIRQPending", ref FIRQPending);
			ser.Sync("IRQPending", ref IRQPending);

			ser.Sync("indexed_op", ref indexed_op);
			ser.Sync("indexed_reg", ref indexed_reg);
			ser.Sync("indexed_op_reg", ref indexed_op_reg);
			ser.Sync("temp", ref temp);

			ser.Sync("instr_pntr", ref instr_pntr);
			ser.Sync("cur_instr", ref cur_instr, false);
			ser.Sync("opcode_see", ref opcode_see);
			ser.Sync("IRQS", ref IRQS);
			ser.Sync("irq_pntr", ref irq_pntr);

			ser.Sync("Regs", ref Regs, false);
			ser.Sync("TotalExecutedCycles", ref TotalExecutedCycles);

			ser.EndSection();
		}
	}
}