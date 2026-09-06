using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace KMU.HisOrder.MVC.Models
{
    public class ChartRelated
    {
        public List<string> cloumns { get; set; }
        public List<List<string>> mcloumns { get; set; }
        public List<string> categories { get; set; }
        public bool HaveData { get; set; }
    }

    public class monthlypiechart {

        public string code { get; set; }
        public string department { get; set; }
        public int Total { get; set; }
    }


    public class PatientCountIng
    {
        public string DepCode { get; set; }
        public string Department { get; set; }
        public string RegNoon { get; set; }
        public string Sex { get; set; }
        public short RegSeqNo { get; set; }
        //public  DateOnly RegDate { get; set; }
        public string RegStatus { get; set; }
        public string Inhospid { get; set; }
        public string RegPatientId { get; set; }
        public string YearMon { get; set; }
        public string dpt_category { get; set; }
    }

    public class DhisTwo
    {
        public int age { get; set; }
        public string IcdCode { get; set; }
        public int? Dhis2Code { get; set; }
        public string Diseases { get; set; }
        public int? ShowSeq { get; set; }
        public string Inhospid { get; set; }
        public string ChrPatientId { get; set; }
        public string HplanType { get; set; }
        public string PlanCode { get; set; }
    }

    public class DhisTwoCounting
    {
        public int overFive { get; set; }
        public int underFive { get; set; }
        public int total { get; set; }
        public int? dhis2Code { get; set; }
        public string diseases { get; set; }
        public int? showSeq { get; set; }
    }

    public class PieChart
    {
        public string PieId { get; set; }
        public string PieName { get; set; }
        public List<List<string>> Cloumns { get; set; }
        public string dpt_category { get; set; }
    }

    public class DeptForChart : ClinicSchedule
    {
        public string dpt_category { get; set; }
    }
    public class ReloadChart
    {
        public List<PieChart> PieChartList { get; set; }
        public int Total { get; set; }
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }
        public int CompletedCount { get; set; }
        //public int TotalOPD { get; set; }
        //public int TotalER { get; set; }
        //public int MaleOPD { get; set; }
        //public int MaleER { get; set; }
        //public int FemaleOPD { get; set; }
        //public int FemaleER { get; set; }
        //public int CompletedOPD { get; set; }
        //public int CompletedER { get; set; }
        public List<PatientCountIng> pList { get; set; }

    }

    public class cardreload
    {
        public int Total { get; set; }
        public int maleCount { get; set; }
        public int femaleCount { get; set; }
        public int finished { get; set; }
    }
    public class multipleSelect
    {
        public string type { get; set; }
        public string Label { get; set; }
        public List<ChildSelect> Child { get; set; }


        public multipleSelect()
        {
            this.type = "optgroup";
        }

    }

    public class ChildSelect
    {
        public string text { get; set; }
        public string value { get; set; }
    }
}
