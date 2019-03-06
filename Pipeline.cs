using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class Pipeline : MonoBehaviour
{
    #region variable
    public Value VA1;
    public Value2 VA2;
    public Animator anim1, anim2, anim3, anim4;
    public int point = 0;
    const int IHALT = 0;
    const int INOP = 1;
    const int IRRMOVL = 2;
    const int IIRMOVL = 3;
    const int IRMMOVL = 4;
    const int IMRMOVL = 5;
    const int IOPL = 6;
    const int IJXX = 7;
    const int ICALL = 8;
    const int IRET = 9;
    const int IPUSHL = 0xA;
    const int IPOPL = 0xB;
    const int FNONE = 0;
    const int ALUADD = 0;
    const int SAOK = 1;
    const int SADR = 2;
    const int SINS = 3;
    const int SHLT = 4;
    const int SBUB = 5;
    const int SSTA = 6;
    const int REAX = 0;
    const int RECX = 1;
    const int REDX = 2;
    const int REBX = 3;
    const int RESP = 4;
    const int REBP = 5;
    const int RESI = 6;
    const int REDI = 7;
    const int RNONE = 0xF;
    const int MAX_SIZE = (1 << 16);//貌似不能2^32会变成负数..
    const int MWRITE = 1;
    const int MREAD = 2;
    public byte[] mem = new byte[MAX_SIZE];
    bool pc_error, instruct_error;
    string[] regwrite = { "%eax", "%ecx", "%edx", "%ebx", "%esp", "%ebp", "%esi", "%edi" };
    public int F_predPC;
    public int f_stat, f_icode, f_ifun, f_valC, f_valP, f_pc, f_predPC, f_rA, f_rB, rgneed;
    public bool regneed, valCneed;
    public int D_stat, D_icode, D_ifun, D_rA, D_rB, D_valC, D_valP;
    public int d_stat, d_icode, d_ifun, d_valC, d_valA, d_valB, d_dstE, d_dstM, d_srcA, d_srcB;
    public int E_stat, E_icode, E_ifun, E_valC, E_valA, E_valB, E_dstE, E_dstM, E_srcA, E_srcB;
    public int e_stat, e_icode, e_valE, e_valA, e_dstE, e_dstM;
    public bool e_Cnd, set_cc;
    public int ALU_A, ALU_B;
    public int ALU_fun;
    public int M_stat, M_icode, M_valE, M_valA, M_dstE, M_dstM;
    public bool M_Cnd;
    public int m_stat, m_icode, m_valE, m_valM, m_dstE, m_dstM, m_address, m_control;
    bool m_error;
    public int W_stat, W_icode, W_valE, W_valM, W_dstE, W_dstM;
    public int Stat;
    public int w_dstE, w_valE, w_dstM, w_valM;
    public bool ZF, SF, OF;
    bool F_stop, F_bubble, D_stop, D_bubble, E_stop, E_bubble, M_stop, M_bubble, W_stop, W_bubble;
    string D_write_A, D_write_B;
    public int[] reg;
    int cycle = 0,wsf,wof,wzf,wecnd;
    bool isrun = false, isbegin = false;
    double time1 = 0, time2 = 0;
    string tempp;

    void initialize()
    {
        time1 = time2 = 0;
        pc_error = instruct_error = regneed = valCneed = false;
        f_icode = 1; F_predPC = f_ifun = f_valC = f_valP = f_pc = f_predPC = 0;
        D_icode = 1; D_ifun = D_valC = D_valP = 0;
        d_icode = 1; d_ifun = d_valC = d_valA = d_valB = 0;
        E_icode = 1; E_ifun = E_valC = E_valA = E_valB = 0;
        e_icode = 1; e_valE = 0;
        ALU_A = ALU_B = ALU_fun = 0;
        M_icode = 1; M_valE = M_valA = 0;
        m_icode = 1; m_valE = m_valM = m_control = m_address = 0;
        W_icode = 1; W_valE = W_valM = 0;
        D_rA = D_rB = d_dstE = d_dstM = d_srcA = d_srcB = RNONE;
        E_dstE = E_dstM = E_srcA = E_srcB = e_dstE = e_dstM = RNONE;
        M_dstE = M_dstM = m_dstE = m_dstM = RNONE;
        W_dstE = W_dstM = RNONE;
        f_stat = D_stat = d_stat = E_stat = e_stat = M_stat = m_stat = W_stat = Stat = SAOK;
        f_rA = f_rB = REAX;
        m_error = ZF = SF = OF = e_Cnd = M_Cnd = set_cc = false;
        F_stop = F_bubble = D_stop = D_bubble = E_stop = E_bubble = M_bubble = M_stop = W_bubble = W_stop = false;
        reg = new int[9];
        D_write_A = D_write_B = "N";
        for (int i = 0; i < 9; i++)
            reg[i] = 0;
    }
    #endregion
    #region read
    int tot_instr = 0;
    public void read(string filepath)
    {
        FileStream file = new FileStream(filepath, FileMode.Open, FileAccess.Read);
        BinaryReader data = new BinaryReader(file);
        for (int kk = 0; kk < tot_instr; kk++)
            mem[kk] = 0;
        int length = (int)file.Length;
        int i = 0;
        byte temp = 0;
        while (length > 0)
        {
            temp = 0;
            for (int k = 0; k < 2; k++)
            {
                byte tempByte = data.ReadByte();
                if (tempByte >= '0' && tempByte <= '9')
                    temp += (byte)(tempByte - '0');
                else if (tempByte >= 'A' && tempByte <= 'F')
                    temp += (byte)(tempByte - 'A' + 10);
                else
                    temp += (byte)(tempByte - 'a' + 10);
                
                if (k == 0) temp *= 16;
                length--;
            }
            mem[i] = temp;
            i++;
        }
        tot_instr = i;
        file.Close();
        data.Close();
    }
    #endregion
    #region Fetch
    void Fetch()
    {
        if (W_icode == IRET)
        {
            if (point == 0)
                GameObject.Find("begin44").GetComponent<plane6>().brun();
            f_pc = W_valM;
        }
        else if (M_icode == IJXX && !M_Cnd)
        {
            if (point == 0)
                GameObject.Find("begin41").GetComponent<plane3>().brun();
            f_pc = M_valA;
        }
        else
        {
            f_pc = F_predPC;
            if (point == 0)
            {
                GameObject.Find("begin1").GetComponent<car1>().brun();
                GameObject.Find("begin2").GetComponent<car2>().brun();
            }
        }

        if (f_pc > MAX_SIZE)
            pc_error = true;
        else
            pc_error = false;
        //Debug.Log(f_pc + " " + mem[f_pc + 1]);
        if (pc_error)
        {
            f_icode = INOP;
            f_ifun = FNONE;
        }
        else
        {
            f_icode = (byte)((mem[f_pc] >> 4) & (0xf));
            f_ifun = (byte)(mem[f_pc] & (0xf));
            if (point == 0)
            {
                GameObject.Find("begin9").GetComponent<car9>().brun();
                GameObject.Find("begin8").GetComponent<car8>().brun();
            }
        }
        if (f_icode >= 0 && f_icode <= 11)
        {
            instruct_error = false;
            if ((f_icode == IOPL && f_ifun > 3) || (f_icode == IJXX && f_ifun > 6))
                instruct_error = true;
        }
        else
            instruct_error = true;

        if (pc_error)
            f_stat = SADR;
        else if (instruct_error)
            f_stat = SINS;
        else if (f_icode == IHALT)
            f_stat = SHLT;
        else
            f_stat = SAOK;

        if (f_icode == IRRMOVL || f_icode == IOPL || f_icode == IPUSHL || f_icode == IPOPL || f_icode == IIRMOVL || f_icode == IRMMOVL || f_icode == IMRMOVL)
            regneed = true;
        else
            regneed = false;

        if (f_icode == IIRMOVL || f_icode == IRMMOVL || f_icode == IMRMOVL || f_icode == IJXX || f_icode == ICALL)
            valCneed = true;
        else
            valCneed = false;
        if (regneed)
        {
            if (point == 0)
            {
                GameObject.Find("begin6").GetComponent<car6>().brun();
                GameObject.Find("begin7").GetComponent<car7>().brun();
            }
            f_rA = (byte)((mem[f_pc + 1] >> 4) & (0xf));
            f_rB = (byte)(mem[f_pc + 1] & (0xf));
            rgneed = 1;
        }
        else 
        {
            f_rA = RNONE;
            f_rB = RNONE;
            rgneed = 0;
        }
        f_valC = 0;
        if (valCneed)
        {
            if (point == 0)
            {
                GameObject.Find("begin5").GetComponent<car5>().brun();
            }
            for (int i = 4; i > 0; i--)
            {
                //if (f_icode == 8) Debug.Log("haha" + i +" "+f_valC+ " " + mem[f_pc + i]);
                f_valC = (f_valC << 8) + mem[f_pc + rgneed + i];
            }
        }
        f_valP = f_pc + 1;
        if (valCneed && regneed)
            f_valP = f_pc + 6;
        if (valCneed && !regneed)
            f_valP = f_pc + 5;
        if (!valCneed && regneed)
            f_valP = f_pc + 2;
        if (point == 0)
        {
            GameObject.Find("begin4").GetComponent<car4>().brun();
            GameObject.Find("begin11").GetComponent<car11>().brun();
        }
        if (f_icode == IJXX || f_icode == ICALL)
        {
            if (point == 0)
            {
                GameObject.Find("begin12").GetComponent<car12>().brun();
            }
            f_predPC = f_valC;
        }
        else
        {
            if (point == 0)
            {
                GameObject.Find("begin10").GetComponent<car10>().brun();
            }
            f_predPC = f_valP;
        }
        //Debug.Log("f_icode f_valC " + f_icode + " " + f_valC);
    }
    #endregion
    #region Decode
    void Decode()
    {
        d_stat = D_stat;
        d_icode = D_icode;
        d_ifun = D_ifun;
        d_valC = D_valC;
        if (point == 0)
        {
            GameObject.Find("begin13").GetComponent<car13>().brun();
            GameObject.Find("begin14").GetComponent<car14>().brun();
            GameObject.Find("begin15").GetComponent<car15>().brun();
        }
        d_srcA = d_srcB = d_dstE = d_dstM = RNONE;
        if (D_icode == IRRMOVL || D_icode == IRMMOVL || D_icode == IOPL || D_icode == IPUSHL)
            d_srcA = D_rA;
        if (D_icode == IPOPL || D_icode == IRET)
            d_srcA = RESP;

        if (D_icode == IOPL || D_icode == IRMMOVL || D_icode == IMRMOVL)
            d_srcB = D_rB;
        if (D_icode == IPUSHL || D_icode == IPOPL || D_icode == ICALL || D_icode == IRET)
            d_srcB = RESP;

        if (D_icode == IRRMOVL || D_icode == IIRMOVL || D_icode == IOPL)
            d_dstE = D_rB;
        if (D_icode == IPUSHL || D_icode == IPOPL || D_icode == ICALL || D_icode == IRET)
            d_dstE = RESP;

        if (D_icode == IMRMOVL || D_icode == IPOPL)
            d_dstM = D_rA;
        if (point == 0)
            GameObject.Find("begin20").GetComponent<car20>().brun();
        if (D_icode == ICALL || D_icode == IJXX)
        {
            if (point == 0)
                GameObject.Find("begin16").GetComponent<car16>().brun();
            d_valA = D_valP;
            D_write_A = "NULL";
        }
        else if (d_srcA == RNONE)
        {
            d_valA = 0;
            D_write_A = "NULL";
        }
        else if (d_srcA == e_dstE)
        {
            if (point == 0)
            {
                GameObject.Find("begin19").GetComponent<car19>().brun();
                GameObject.Find("begin39").GetComponent<plane1>().brun();
            }
            d_valA = e_valE;
            D_write_A = "valA <- e_valE = " + e_valE.ToString();
        }
        else if (d_srcA == m_dstM)
        {
            if (point == 0)
            {
                GameObject.Find("begin19").GetComponent<car19>().brun();
                GameObject.Find("begin42").GetComponent<plane4>().brun();
            }
            d_valA = m_valM;
            D_write_A = "valA <- m_valM = " + m_valM.ToString();
        }
        else if (d_srcA == m_dstE)
        {
            if (point == 0)
            {
                GameObject.Find("begin19").GetComponent<car19>().brun();
                GameObject.Find("begin44").GetComponent<plane6>().brun();
            }
            d_valA = M_valE;
            D_write_A = "valA <- m_valE = " + M_valE.ToString();
        }
        else if (d_srcA == w_dstM)
        {
            if (point == 0)
            {
                GameObject.Find("begin19").GetComponent<car19>().brun();
                GameObject.Find("begin42").GetComponent<plane4>().brun();
            }
            d_valA = W_valM;
            D_write_A = "valA <- w_valM = " + W_valM.ToString();
        }
        else if (d_srcA == w_dstE)
        {
            if (point == 0)
            {
                GameObject.Find("begin19").GetComponent<car19>().brun();
                GameObject.Find("begin44").GetComponent<plane6>().brun();
            }
            d_valA = W_valE;
            D_write_A = "valA <- w_valE = " + W_valE.ToString();
        }
        else
        {
            if (point == 0)
            {
                GameObject.Find("begin17").GetComponent<car17>().brun();
            }
            d_valA = reg[d_srcA];
            D_write_A = "valA <- R[" + regwrite[d_srcA] + "] = " + d_valA.ToString();
        }
        if (point == 0)
            GameObject.Find("begin21").GetComponent<car21>().brun();
        if (d_srcB == RNONE)
        {   
            d_valB = 0;
            D_write_B = "NULL";
        }
        else if (d_srcB == e_dstE)
        {
            if (point == 0)
                GameObject.Find("begin39").GetComponent<plane1>().brun();
            d_valB = e_valE;
            D_write_B = "valB <- e_valE = " + e_valE.ToString();
        }
        else if (d_srcB == m_dstM)
        {
            if (point == 0)
                GameObject.Find("begin42").GetComponent<plane4>().brun();
            d_valB = m_valM;
            D_write_B = "valB <- m_valM = " + m_valM.ToString();
        }
        else if (d_srcB == m_dstE)
        {
            if (point == 0)
            GameObject.Find("begin44").GetComponent<plane6>().brun();
            d_valB = m_valE;
            D_write_B = "valB <- m_valE = " + M_valE.ToString();
        }
        else if (d_srcB == w_dstM)
        {
            if (point == 0)
            GameObject.Find("begin42").GetComponent<plane4>().brun();
            d_valB = w_valM;
            D_write_B = "valB <- w_valM = " + W_valM.ToString();
        }
        else if (d_srcB == w_dstE)
        {
            if (point == 0)
            GameObject.Find("begin44").GetComponent<plane6>().brun();
            d_valB = w_valE;
            D_write_B = "valB <- w_valE = " + W_valE.ToString();
        }
        else
        {
            if (point == 0)
            GameObject.Find("begin18").GetComponent<car18>().brun();
            d_valB = reg[d_srcB];
            D_write_B = "valB <- R[" + regwrite[d_srcB] + "] = " + d_valB.ToString();
        }
        //Debug.Log("d_dstE d_dstM " + d_dstE + " " + d_dstM);
    }
    #endregion
    #region Execute
    void Execute()
    {
        e_stat = E_stat;
        e_icode = E_icode;
        e_valA = E_valA;
        e_dstM = E_dstM; 
        e_dstE = E_dstE;
        if (point == 0)
        {
            GameObject.Find("begin22").GetComponent<car22>().brun();
            GameObject.Find("begin25").GetComponent<car25>().brun();
            GameObject.Find("begin32").GetComponent<car32>().brun();
            GameObject.Find("begin31").GetComponent<car31>().brun();
        }
        ALU_A = 0;
        if (E_icode == IIRMOVL || E_icode == IMRMOVL || E_icode == IRMMOVL)
        {
            if (point == 0)
            GameObject.Find("begin23").GetComponent<car23>().brun();
            ALU_A = E_valC;
        }
        if (E_icode == IRRMOVL || E_icode == IOPL)
        {
            if (point == 0)
            GameObject.Find("begin24").GetComponent<car24>().brun();
            ALU_A = E_valA;
        }
        if (E_icode == ICALL || E_icode == IPUSHL)
        {
            ALU_A = -4;
        }
        if (E_icode == IRET || E_icode == IPOPL)
            ALU_A = 4;

        ALU_B = 0;
        if (E_icode == IRMMOVL || E_icode == IMRMOVL || E_icode == IOPL || E_icode == ICALL || E_icode == IPUSHL || E_icode == IRET || E_icode == IPOPL)
        {
            if (point == 0)
            GameObject.Find("begin26").GetComponent<car26>().brun();
            ALU_B = E_valB;
        }

        if (E_icode == IOPL)
            ALU_fun = E_ifun;
        else
            ALU_fun = ALUADD;
        if (point == 0)
        GameObject.Find("begin29").GetComponent<car29>().brun();
        switch (ALU_fun)
        {
            case 0: e_valE = ALU_B + ALU_A; break;
            case 1: e_valE = ALU_B - ALU_A; break;
            case 2: e_valE = ALU_B & ALU_A; break;
            case 3: e_valE = ALU_B ^ ALU_A; break;
            default: e_valE = 0; break;
        }
        if (point == 0)
        {
            GameObject.Find("begin27").GetComponent<car27>().brun();
            GameObject.Find("begin28").GetComponent<car28>().brun();
            GameObject.Find("begin30").GetComponent<car30>().brun();
        }
        if (e_icode == IOPL)
            set_cc = true;
        else
            set_cc = false;

        if (set_cc)
        {
            if (point == 0)
                GameObject.Find("begin45").GetComponent<r1>().brun();
            if (e_valE < 0)
                SF = true;
            else
                SF = false;
            if (e_valE == 0)
                ZF = true;
            else
                ZF = false;
            if (!((ALU_A < 0) ^ (ALU_B < 0)) && ((ALU_A < 0) ^ (e_valE < 0)))
                OF = true;
            else
                OF = false;
        }
        if (E_icode == IJXX)
        {
            if (point == 0)
                GameObject.Find("begin46").GetComponent<r2>().brun();
            switch (E_ifun)
            {
                case 0: e_Cnd = true; break;
                case 1: if (ZF || SF) e_Cnd = true; else e_Cnd = false; break;
                case 2: if (SF) e_Cnd = true; else e_Cnd = false; break;
                case 3: if (ZF) e_Cnd = true; else e_Cnd = false; break;
                case 4: if (!ZF) e_Cnd = true; else e_Cnd = false; break;
                case 5: if (!SF) e_Cnd = true; else e_Cnd = false; break;
                case 6: if (!ZF && !SF) e_Cnd = true; else e_Cnd = false; break;
            }
        }
        else
            e_Cnd = false;
        //Debug.Log("e_vale = " + e_valE + "  cy = " + cycle );
        //Debug.Log("e_vala = " + e_valA + "  cy = " + cycle);
    }
    #endregion
    #region Memory
    void Memory()
    {
        m_icode = M_icode;
        if (point == 0)
        GameObject.Find("begin33").GetComponent<car33>().brun();
        m_valE = M_valE;
        if (point == 0)
        GameObject.Find("begin34").GetComponent<car34>().brun();
        m_dstE = M_dstE;
        if (point == 0)
        GameObject.Find("begin37").GetComponent<car37>().brun();
        m_dstM = M_dstM;
        if (point == 0)
        GameObject.Find("begin38").GetComponent<car38>().brun();
        if (M_icode == IRMMOVL || M_icode == IMRMOVL || M_icode == IPUSHL || M_icode == ICALL)
            m_address = M_valE;
        if (M_icode == IPOPL || M_icode == IRET)
            m_address = M_valA;
        m_control = 0;
        if (M_icode == IMRMOVL || M_icode == IPOPL || M_icode == IRET)
            m_control = MREAD;
        if (M_icode == IRMMOVL || M_icode == IPUSHL || M_icode == ICALL)
            m_control = MWRITE;
        if (m_address > MAX_SIZE)
            m_error = true;
        else
            m_error = false;
        //Debug.Log(cycle + " adress = " + m_address + " " + m_control);
        if (m_error == false)
        {
            m_stat = M_stat;
            m_valM = 0;
            if (m_control == MREAD)
            {
                if (point == 0)
                GameObject.Find("begin36").GetComponent<car36>().brun();
                for (int i = 3; i >= 0; i--)
                {
                    m_valM = (m_valM << 8) + mem[m_address + i];
                }
            }
            if (m_control == MWRITE)
            {
                if (point == 0)
                GameObject.Find("begin35").GetComponent<car35>().brun();
                for (int i = 0; i < 4; i++)
                    mem[m_address + i] = (byte)((M_valA >> (8 * i)) & 0xff);
            }
        }
        else
            m_stat = SADR;
    }
    #endregion
    #region Write
    void Write()
    {
        w_valE = W_valE;
        w_valM = W_valM;
        w_dstE = W_dstE;
        w_dstM = W_dstM;
        
        if (W_icode == IHALT)
            Stat = SHLT;
        else if (W_stat == SBUB)
            Stat = SAOK;
        else
            Stat = W_stat;
        //Debug.Log("w_dstE w_dstM " + W_dstE + " " + W_dstM);
        if (Stat == SAOK)
        {
            if (W_dstE != RNONE)
            {
                if (point == 0)
                    GameObject.Find("begin44").GetComponent<plane6>().brun();
                reg[W_dstE] = W_valE;
            }
            if (W_dstM != RNONE)
            {
                if (point == 0)
                    GameObject.Find("begin42").GetComponent<plane4>().brun();
                reg[W_dstM] = W_valM;
            }
        }
    }
    #endregion
    #region Control
    void stop_bub_control()
    {
        F_bubble = false;
        E_stop = false;
        M_stop = false;
        W_bubble = false;
        if ((E_icode == IMRMOVL || E_icode == IPOPL) && (E_dstM == d_srcA || E_dstM == d_srcB) || (IRET == D_icode || IRET == E_icode || IRET == M_icode))
            F_stop = true;
        else
            F_stop = false;

        if ((E_icode == IMRMOVL || E_icode == IPOPL) && (E_dstM == d_srcA || E_dstM == d_srcB))
            D_stop = true;
        else
            D_stop = false;
        if ((E_icode == IJXX && !e_Cnd) || !((E_icode == IMRMOVL || E_icode == IPOPL) && (E_dstM == d_srcA || E_dstM == d_srcB)) && (IRET == D_icode || IRET == E_icode || IRET == M_icode))
            D_bubble = true;
        else
            D_bubble = false;
        if ((E_icode == IJXX && !e_Cnd) || (E_icode == IMRMOVL || E_icode == IPOPL) && (E_dstM == d_srcA || E_dstM == d_srcB))
            E_bubble = true;
        else
            E_bubble = false;

        if (m_stat == SADR || m_stat == SINS || m_stat == SHLT || W_stat == SADR || W_stat == SINS || W_stat == SHLT)
            M_bubble = true;
        else
            M_bubble = false;

        if (W_stat == SADR || W_stat == SINS || W_stat == SHLT)
            W_stop = true;
        else
            W_stop = false;
    }
    void Continue()
    {
        cycle++;
        Write();
        Memory();
        Execute();
        Decode();
        Fetch();
        stop_bub_control();
        if (!F_stop)
        {
            F_predPC = f_predPC;
        }
        if (!D_stop)
        {
            D_stat = f_stat; D_icode = f_icode; D_ifun = f_ifun; D_rA = f_rA; D_rB = f_rB; D_valC = f_valC; D_valP = f_valP;
        }
        if (D_bubble)
        {
            D_stat = SBUB; D_icode = INOP; D_ifun = FNONE; D_rA = RNONE; D_rB = RNONE; D_valC = 0; D_valP = 0;
        }
        if (E_bubble)
        {
            E_stat = SBUB; E_icode = INOP; E_ifun = FNONE; E_valC = 0; E_valA = 0; E_valB = 0; E_dstE = RNONE; E_dstM = RNONE; E_srcA = RNONE; E_srcB = RNONE;
        }
        else
        {
            E_stat = d_stat; E_icode = d_icode; E_ifun = d_ifun; E_valC = d_valC; E_valA = d_valA; E_valB = d_valB; E_dstE = d_dstE; E_dstM = d_dstM; E_srcA = d_srcA; E_srcB = d_srcB;
        }
        if (M_bubble)
        {
            M_stat = SBUB; M_icode = INOP; M_Cnd = false; M_valE = 0; M_valA = 0; M_dstE = RNONE; M_dstM = RNONE;
        }
        else
        {
            M_stat = e_stat; M_icode = e_icode; M_Cnd = e_Cnd; M_valE = e_valE; M_valA = e_valA; M_dstE = e_dstE; M_dstM = e_dstM;
        }
        if (!W_stop)
        {
            W_stat = m_stat; W_icode = m_icode; W_valE = m_valE; W_valM = m_valM; W_dstE = m_dstE; W_dstM = m_dstM;
        }
    }

    #endregion
    #region WriteUI
    void F_write(StringWriter sw)
    {
        if (F_stop)
        {
            sw.WriteLine("NOP");
            return;
        }
        if (pc_error) sw.WriteLine("pc_error");
        else if (instruct_error) sw.WriteLine("instruct_error");
        else sw.WriteLine("icode:ifun <- M[0x{0:x}] = {1}:{2}", f_pc, f_icode, f_ifun);

        if (regneed)
            sw.WriteLine("rA:rB <- M[0x{0:x}] = {1}:{2}", f_pc + 1, f_rA, f_rB);
        if (valCneed)
            sw.WriteLine("valC <- M[0x{0:x}] = 0x{1:x}", f_pc + 1 + rgneed, f_valC);
        sw.WriteLine("valP <- PC + {0} = 0x{1:x}", f_valP - f_pc, f_valP);
    }
    void D_write(StringWriter sw)
    {
        if (D_bubble || D_stop)
        {
            sw.WriteLine("NOP");
            return;
        }
        if (D_write_A != "NULL")
            sw.WriteLine(D_write_A);
        if (D_write_B != "NULL")
            sw.WriteLine(D_write_B);
    }
    void E_write(StringWriter sw)
    {
        if (E_bubble || E_stop)
        {
            sw.WriteLine("NOP");
            return;
        }
        switch (ALU_fun)
        {
            case 0: sw.WriteLine("valE <- aluB + aluA = {0}", e_valE); break;
            case 1: sw.WriteLine("valE <- aluB - aluA = {0}", e_valE); break;
            case 2: sw.WriteLine("valE <- aluB & aluA = {0}", e_valE); break;
            case 3: sw.WriteLine("valE <- aluB ^ aluA = {0}", e_valE); break;
            default: sw.WriteLine("e_valE = 0"); break;
        }
        if (SF) wsf = 1; else wsf = 0;
        if (ZF) wzf = 1; else wzf = 0;
        if (OF) wof = 1; else wof = 0;
        if (e_Cnd) wecnd = 1; else wecnd = 0;

        if (set_cc)
        {
            tempp = "Cnd <- Cond({" + wsf.ToString() + "," + wzf.ToString() + "," + wof.ToString() + "}," + E_ifun.ToString() + ") = " + wecnd.ToString();
            sw.WriteLine(tempp);
            //sw.WriteLine("Cnd <- Cond({0x{0:x},0x{1:x},0x{2:x}},0x{3:x}) = 0x{4:x}", wsf, wzf, wof, E_ifun, wecnd);
        }
    }
    void M_write(StringWriter sw)
    {
        if (M_bubble || M_stop)
        {
            sw.WriteLine("NOP");
            return;
        }
        if (m_control == MWRITE)
            sw.WriteLine("M[0x{0:x}] <- {1}", m_address, M_valA);
        if (m_control == MREAD)
            sw.WriteLine("valM <- M[0x{0:x}] = {1}", m_address, m_valM);
    }
    void W_write(StringWriter sw)
    {
        if (W_stop)
        {
            sw.WriteLine("NOP");
            return;
        }
        if (Stat == SAOK)
        {
            if (W_dstE != RNONE)
            {
                tempp = "R[" + regwrite[W_dstE] + "] <- W_valE = " + w_valE.ToString();
                sw.WriteLine(tempp);
            }
                //sw.WriteLine("R[" + regwrite[W_dstE] + "] <- W_valE = 0x{0:x8}", w_valE);
            if (W_dstM != RNONE)
            {
                tempp = "R[" + regwrite[W_dstM] + "] <- W_valM = " + w_valM.ToString();
                sw.WriteLine(tempp);
            }
                //sw.WriteLine("R[" + regwrite[W_dstE] + "] <- W_valM = 0x{0:x8}", w_valM);
        }
    }
    void PC_write(StringWriter sw)
    {
        if (M_icode == IJXX && !M_Cnd)
            sw.WriteLine("PC <- M[valA] = 0x{0:x}", f_pc);
        else if (W_icode == IRET)
            sw.WriteLine("PC <- M[valM] = 0x{0:x}", f_pc);
        else
            sw.WriteLine("PC <- F_predPC = 0x{0:x}", f_pc);
    }
    public string WriteUI(int i)
    {
        if (i == 0) return "";
        StringWriter sw = new StringWriter();
        sw.WriteLine("Cycle {0}", i);
        sw.WriteLine("--------------------------------");
        sw.WriteLine("FETCH:");
        F_write(sw);
        sw.WriteLine("--------------------------------");
        sw.WriteLine("DECODE:");
        D_write(sw);
        sw.WriteLine("--------------------------------");
        sw.WriteLine("EXECUTE:");
        E_write(sw);
        sw.WriteLine("--------------------------------");
        sw.WriteLine("MEMORY:");
        M_write(sw);
        sw.WriteLine("--------------------------------");
        sw.WriteLine("WRITE:");
        W_write(sw);
        sw.WriteLine("--------------------------------");
        sw.WriteLine("PC:");
        PC_write(sw);
        return sw.ToString();
    }
    #endregion

    void setstat()
    {   
        anim1.SetFloat("DL", 0); anim1.SetFloat("BL", 0); anim1.SetFloat("RL", 0);
        anim1.SetFloat("DB", 0); anim1.SetFloat("LB", 0); anim1.SetFloat("RB", 0);
        anim1.SetFloat("LD", 0); anim1.SetFloat("BD", 0); anim1.SetFloat("RD", 0);
        anim1.SetFloat("DR", 0); anim1.SetFloat("LR", 0); anim1.SetFloat("BR", 0);
        anim2.SetFloat("DL", 0); anim2.SetFloat("BL", 0); anim2.SetFloat("RL", 0);
        anim2.SetFloat("DB", 0); anim2.SetFloat("LB", 0); anim2.SetFloat("RB", 0);
        anim2.SetFloat("LD", 0); anim2.SetFloat("BD", 0); anim2.SetFloat("RD", 0);
        anim2.SetFloat("DR", 0); anim2.SetFloat("LR", 0); anim2.SetFloat("BR", 0);
        anim3.SetFloat("DL", 0); anim3.SetFloat("BL", 0); anim3.SetFloat("RL", 0);
        anim3.SetFloat("DB", 0); anim3.SetFloat("LB", 0); anim3.SetFloat("RB", 0);
        anim3.SetFloat("LD", 0); anim3.SetFloat("BD", 0); anim3.SetFloat("RD", 0);
        anim3.SetFloat("DR", 0); anim3.SetFloat("LR", 0); anim3.SetFloat("BR", 0);
        anim4.SetFloat("DL", 0); anim4.SetFloat("BL", 0); anim4.SetFloat("RL", 0);
        anim4.SetFloat("DB", 0); anim4.SetFloat("LB", 0); anim4.SetFloat("RB", 0);
        anim4.SetFloat("LD", 0); anim4.SetFloat("BD", 0); anim4.SetFloat("RD", 0);
        anim4.SetFloat("DR", 0); anim4.SetFloat("LR", 0); anim4.SetFloat("BR", 0);
        switch (D_stat)
        {
            case 1: anim1.SetFloat("DL", 1); anim1.SetFloat("BL", 1); anim1.SetFloat("RL", 1); break;
            case 2: anim1.SetFloat("LD", 1); anim1.SetFloat("BD", 1); anim1.SetFloat("RD", 1); break;
            case 3: anim1.SetFloat("LD", 1); anim1.SetFloat("BD", 1); anim1.SetFloat("RD", 1); break;
            case 4: anim1.SetFloat("DR", 1); anim1.SetFloat("LR", 1); anim1.SetFloat("BR", 1); break;
            default: anim1.SetFloat("DB", 1); anim1.SetFloat("LB", 1); anim1.SetFloat("RB", 1); break;
        }
        switch (E_stat)
        {
            case 1: anim2.SetFloat("DL", 1); anim2.SetFloat("BL", 1); anim2.SetFloat("RL", 1); break;
            case 2: anim2.SetFloat("LD", 1); anim2.SetFloat("BD", 1); anim2.SetFloat("RD", 1); break;
            case 3: anim2.SetFloat("LD", 1); anim2.SetFloat("BD", 1); anim2.SetFloat("RD", 1); break;
            case 4: anim2.SetFloat("DR", 1); anim2.SetFloat("LR", 1); anim2.SetFloat("BR", 1); break;
            default: anim2.SetFloat("DB", 1); anim2.SetFloat("LB", 1); anim2.SetFloat("RB", 1); break;
        }
        switch (M_stat)
        {
            case 1: anim3.SetFloat("DL", 1); anim3.SetFloat("BL", 1); anim3.SetFloat("RL", 1); break;
            case 2: anim3.SetFloat("LD", 1); anim3.SetFloat("BD", 1); anim3.SetFloat("RD", 1); break;
            case 3: anim3.SetFloat("LD", 1); anim3.SetFloat("BD", 1); anim3.SetFloat("RD", 1); break;
            case 4: anim3.SetFloat("DR", 1); anim3.SetFloat("LR", 1); anim3.SetFloat("BR", 1); break;
            default: anim3.SetFloat("DB", 1); anim3.SetFloat("LB", 1); anim3.SetFloat("RB", 1); break;
        }
        switch (W_stat)
        {
            case 1: anim4.SetFloat("DL", 1); anim4.SetFloat("BL", 1); anim4.SetFloat("RL", 1); break;
            case 2: anim4.SetFloat("LD", 1); anim4.SetFloat("BD", 1); anim4.SetFloat("RD", 1); break;
            case 3: anim4.SetFloat("LD", 1); anim4.SetFloat("BD", 1); anim4.SetFloat("RD", 1); break;
            case 4: anim4.SetFloat("DR", 1); anim4.SetFloat("LR", 1); anim4.SetFloat("BR", 1); break;
            default: anim4.SetFloat("DB", 1); anim4.SetFloat("LB", 1); anim4.SetFloat("RB", 1); break;
        }
        return;
    }
    public void run(string filePath)
    {
        cycle = 0;
        initialize();
        read(filePath);
        isrun = true;
        isbegin = false;
        VA2.allUpdate(cycle);
        VA2.triggerUpdate(cycle);
    }

    void Update(){
        if (Input.GetKeyDown(KeyCode.R))
            isbegin = true;
        if (Input.GetKeyDown(KeyCode.P))
            isbegin = false;
        if (Input.GetKeyDown(KeyCode.F10))
        {
            isbegin = false;
            time1 += 2;
        }
        if (isrun && isbegin)
        {   
            time1 += Time.deltaTime; 
        }
        if (point > 0 && point > cycle && isbegin)
        {
            if (Stat == SAOK)
            {   
                Continue();
                VA1.allUpdate(cycle);
                VA1.triggerUpdate(cycle);
            }
            else
            {
                VA1.allUpdate(cycle);
                VA1.triggerUpdate(cycle);
                VA2.allUpdate(cycle);
                VA2.triggerUpdate(cycle);
                isbegin = false;
                setstat();
            }
        }
        else
        {   
            if (isbegin && point > 0)
            {
                point = 0;
                time2 = time1;
                isbegin = false;
                VA1.allUpdate(cycle);
                VA1.triggerUpdate(cycle);
                VA2.allUpdate(cycle);
                VA2.triggerUpdate(cycle);
                setstat();
            }
            if (Stat == SAOK && time1 - time2 >= 2)
            {
                time2 = time1;
                Continue();
                VA1.allUpdate(cycle);
                VA1.triggerUpdate(cycle);
                VA2.allUpdate(cycle);
                VA2.triggerUpdate(cycle);
                setstat();
                //Debug.Log(time1+" "+cycle);
                /*Debug.Log("cycle = " + cycle.ToString());
                Debug.Log("eax = " + reg[0]);
                Debug.Log("ecx = " + reg[1]);
                Debug.Log("edx = " + reg[2]);
                Debug.Log("ebx = " + reg[3]);
                //Debug.Log("mem = " + mem[232] + " " + mem[236] + " " + mem[240] + " " + mem[244] + " " + mem[249] + " " + mem[252]);*/
                // WriteUI(cycle);
            }
        }
    }
}