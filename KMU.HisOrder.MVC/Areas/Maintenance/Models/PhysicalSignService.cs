using KMU.HisOrder.MVC.Areas.Maintenance.ViewModels;
using KMU.HisOrder.MVC.Extesion;
using KMU.HisOrder.MVC.Models;

namespace KMU.HisOrder.MVC.Areas.Maintenance.Models
{
    public class PhysicalSignService : IDisposable
    {
        private readonly KMUContext _context;


        public PhysicalSignService(KMUContext context)
        {
            _context = context;
        }

        public void Dispose() => Dispose(disposing: true);

        /// <summary>
        /// Releases all resources currently used by this <see cref="Controller"/> instance.
        /// </summary>
        /// <param name="disposing"><c>true</c> if this method is being invoked by the <see cref="Dispose()"/> method,
        /// otherwise <c>false</c>.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        #region Public Function

        public List<PhysicalSignItem> GetPhysicalSignItems(string reserveType)
        {
            #region Variables setting

            List<PhysicalSignItem> physicalItemList = new List<PhysicalSignItem>();
            List<KmuCoderef> phyCoderefs = new List<KmuCoderef>();
            List<KmuCondition> phyConditions = new List<KmuCondition>();

            #endregion
            try
            {
                phyCoderefs = _context.KmuCoderefs.Where(c => c.RefCodetype == reserveType + "_Physical_Item" &&
                                                              c.RefCasetype == "Y").OrderBy(c => c.RefShowseq).ToList();

                foreach (var code in phyCoderefs)
                {
                    PhysicalSignItem physicalItem = new PhysicalSignItem()
                    {
                        ClinicType = reserveType,
                        ConditonType = code.RefCode,
                        CodeName = code.RefName,
                        InputType = code.RefDes,
                        DefaultFlag = code.RefDefaultFlag == "Y" ? true : false,
                        Conditions = GetPhysicalConditions(code.RefCode)
                    };

                    physicalItemList.Add(physicalItem);
                }
            }
            catch (Exception ex)
            {

            }

            return physicalItemList;
        }

        public TriageReturnClass CaculateTriage(List<PhysicalConditionItem> objConditionDto)
        {
            #region Variable Setting

            TriageReturnClass triage = new TriageReturnClass();
            int triagePoint = 0;

            #endregion
            using (CommonService cService = new CommonService(_context))
            {
                foreach (var condition in objConditionDto)
                {
                    List<KmuCondition> kmuConditions = new List<KmuCondition>();

                    kmuConditions = _context.KmuConditions.Where(c => c.CndCodetype == condition.CODETYPE &&
                                                                      c.CndEnable == 'Y').ToList();

                    foreach (var item in kmuConditions)
                    {
                        bool result1 = false;
                        bool result2 = false;
                        if (item.CndSymbol1.Trim() == "=")
                        {
                            result1 = cService.MAPINFO_CompareForString(condition.VALUE, item.CndValue1, item.CndSymbol1);
                        }
                        else
                        {
                            result1 = cService.MAPINFO_Compare(condition.VALUE, item.CndValue1, item.CndSymbol1);
                        }

                        if (!string.IsNullOrWhiteSpace(item.CndValue2))
                        {
                            if (item.CndSymbol2.Trim() == "=")
                            {
                                result2 = cService.MAPINFO_CompareForString(condition.VALUE, item.CndValue2, item.CndSymbol2);
                            }
                            else
                            {
                                result2 = cService.MAPINFO_Compare(condition.VALUE, item.CndValue2, item.CndSymbol2);
                            }

                            if (result1 && result2)
                            {
                                triagePoint = triagePoint + Convert.ToInt16(item.CndDesc);
                                break;
                            }
                        }
                        else
                        {
                            if (result1)
                            {
                                triagePoint = triagePoint + Convert.ToInt16(item.CndDesc);
                                break;
                            }
                        }

                    }
                }

                triage.TriagePoint = triagePoint.ToString();

                switch (triagePoint)
                {
                    case 0:
                        triage.TriageLevel = "0";
                        triage.lightColor = "white";
                        break;
                    case > 0 and <= 2:
                        triage.TriageLevel = "1";
                        triage.lightColor = "green";
                        break;
                    case > 2 and <= 4:
                        triage.TriageLevel = "2";
                        triage.lightColor = "yellow";
                        break;
                    case > 4 and <= 6:
                        triage.TriageLevel = "3";
                        triage.lightColor = "orange";
                        break;
                    case > 6:
                        triage.TriageLevel = "4";
                        triage.lightColor = "red";
                        break;
                }
            }
            return triage;
        }

        #endregion

        #region Private Function

        private List<KmuCondition> GetPhysicalConditions(string PhysicalType)
        {
            List<KmuCondition> conditionLists = new List<KmuCondition>();

            try
            {
                conditionLists = _context.KmuConditions.Where(c => c.CndCodetype == PhysicalType &&
                                                                 c.CndEnable == 'Y').OrderBy(c => c.CndCode).ToList();
            }
            catch (Exception ex)
            {

            }

            return conditionLists;
        }

        #endregion
    }
}
