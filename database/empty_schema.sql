--
-- PostgreSQL database dump
--

-- Dumped from database version 17.4
-- Dumped by pg_dump version 17.4

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: _monthly_statistics_daily_bases_f(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public._monthly_statistics_daily_bases_f(yyyymm character varying) RETURNS TABLE(reg_date character varying, col1 character varying, col2 character varying, col3 character varying, col4 character varying, col5 character varying)
    LANGUAGE sql
    AS $$

WITH MALE_OVER_5(register_date,male_over_5) as (
select reg.reg_date,count((current_date-chart.chr_birth_date)/365) as MALE_OVER_5 from registration reg join 
public.kmu_chart chart on reg.reg_health_id=chart.chr_health_id
where (reg.reg_status in ('*','T','O'))  and chart.chr_sex ='M' and to_char(reg.reg_date,'YYYY-MM')= YYYYMM
and (current_date-chart.chr_birth_date)/365 >5
group by reg.reg_date
),
FEMALE_OVER_5(register_date, female_over_5) as (
select reg.reg_date,count((current_date-chart.chr_birth_date)/365) as FEMALE_OVER_5 from registration reg join 
public.kmu_chart chart on reg.reg_health_id=chart.chr_health_id
where (reg.reg_status in ('*','T','O')) and chart.chr_sex ='F' and to_char(reg.reg_date,'YYYY-MM')= YYYYMM
and (current_date-chart.chr_birth_date)/365 >5
group by reg.reg_date
),
MALE_UNDER_5(register_date,male_under_5) as (
select reg.reg_date,count((current_date-chart.chr_birth_date)/365) as MALE_UNDER_5 from registration reg join 
public.kmu_chart chart on reg.reg_health_id=chart.chr_health_id
where (reg.reg_status in ('*','T','O'))  and chart.chr_sex ='M' and to_char(reg.reg_date,'YYYY-MM')= YYYYMM
and (current_date-chart.chr_birth_date)/365<=5
group by reg.reg_date
),
FEMALE_UNDER_5(register_date, female_under_5) as (
select reg.reg_date,count((current_date-chart.chr_birth_date)/365) as FEMALE_UNDER_5 from registration reg join 
public.kmu_chart chart on reg.reg_health_id=chart.chr_health_id
where (reg.reg_status in ('*','T','O'))  and chart.chr_sex ='F' and to_char(reg.reg_date,'YYYY-MM')= YYYYMM
and (current_date-chart.chr_birth_date)/365<=5
group by reg.reg_date
),
CTE_FINISHED_PATIENTS(Register_date,FINISHED_PATIENTS) as
(select reg.reg_date,count(reg.reg_status) as FINISHED_PATIENTS from registration reg join 
public.kmu_chart chart on reg.reg_health_id=chart.chr_health_id
where (reg.reg_status in ('*','T','O')) and to_char(reg.reg_date,'YYYY-MM')= YYYYMM
group by reg.reg_date
),
TOTAL_Records(Register_date,female_over_5,male_over_5,femal_under_5,mal_under_5,FINISHED_PATIENTS) AS
(
	select 	TOTAL.register_date,coalesce(fo5.female_over_5,0),coalesce(mo5.male_over_5,0),coalesce(fu5.female_under_5,0),
			coalesce(mu5.male_under_5,0),coalesce(finished.FINISHED_PATIENTS,0)
	from CTE_FINISHED_PATIENTS  TOTAL	
	left outer join CTE_FINISHED_PATIENTS FINISHED on TOTAL.register_date=FINISHED.register_date
	left outer join MALE_OVER_5 mo5 on TOTAL.register_date=mo5.register_date
	left outer join FEMALE_OVER_5 fo5 on TOTAL.register_date=fo5.register_date
	left outer join MALE_UNDER_5 mu5 on TOTAL.register_date=mu5.register_date
	left outer join FEMALE_UNDER_5 fu5 on TOTAL.register_date=fu5.register_date
	
)
select * from TOTAL_Records

$$;


--
-- Name: _monthly_statistics_department_bases_f(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public._monthly_statistics_department_bases_f(yyyymm character varying) RETURNS TABLE(department character varying, col1 character varying, col2 character varying, col3 character varying, col4 character varying, col5 character varying)
    LANGUAGE sql
    AS $$
/*
  Date:2023/09/17
  Coder:Vivienne
  Items: Add status type: O: Observing, and status type : T 
*/
with FINISH_FEMALE_OVER_5(department_name,Female_over_5) as(

select dept.dpt_parent,count((current_date-chr_birth_date)/365)   as age 
from registration reg join public.kmu_department dept on reg.reg_department =dept.dpt_code
join public.kmu_chart  chart on reg.reg_health_id=chart.chr_health_id 
where (reg.reg_status='*' or reg.reg_status='T' or reg.reg_status='O' ) and to_char(reg.reg_date,'YYYY-MM') = yyyymm
and chart.chr_sex='F' and (current_date-chr_birth_date)/365>5
group by dept.dpt_parent
),
FINISH_MALE_OVER_5 (department_name,MALE_OVER_5) as(

select dept.dpt_parent, count((current_date-chr_birth_date)/365)   as age 
from registration reg join public.kmu_department dept on reg.reg_department =dept.dpt_code
join public.kmu_chart  chart on reg.reg_health_id=chart.chr_health_id 
where (reg.reg_status='*' or reg.reg_status='T' or reg.reg_status='O')  and to_char(reg.reg_date,'YYYY-MM') = yyyymm
and chart.chr_sex='M' and (current_date-chr_birth_date)/365>5
group by dept.dpt_parent
),
CTE_OVER_5(DEPARTMENT,female_over_5,MALE_OVER_5) as (
select female.department_name,female.Female_over_5,male.MALE_OVER_5 from FINISH_FEMALE_OVER_5 female join 
FINISH_MALE_OVER_5 male on female.department_name=male.department_name
),

FINISH_FEMALE_UNDER_5(department_name,Female_under_5) as(

select dept.dpt_parent,count((current_date-chr_birth_date)/365)   as age 
from registration reg join public.kmu_department dept on reg.reg_department =dept.dpt_code
join public.kmu_chart  chart on reg.reg_health_id=chart.chr_health_id 
where (reg.reg_status='*' or reg.reg_status='T' or reg.reg_status='O')  and to_char(reg.reg_date,'YYYY-MM') = yyyymm
and chart.chr_sex='F' and (current_date-chr_birth_date)/365<=5
group by dept.dpt_parent
),
FINISH_MALE_UNDER_5(department_name,MALE_under_5) as(

select dept.dpt_parent, count((current_date-chr_birth_date)/365)   as age 
from registration reg join public.kmu_department dept on reg.reg_department =dept.dpt_code
join public.kmu_chart  chart on reg.reg_health_id=chart.chr_health_id 
where (reg.reg_status='*' or reg.reg_status='T' or reg.reg_status='O')  and to_char(reg.reg_date,'YYYY-MM') = yyyymm
and chart.chr_sex='M' and (current_date-chr_birth_date)/365<=5
group by dept.dpt_parent),
CTE_UNDER_5(DEPARTMENT,female_under_5,MALE_under_5) as (
select female.department_name,female.Female_under_5,male.MALE_under_5 from FINISH_FEMALE_UNDER_5 female join 
FINISH_MALE_UNDER_5 male on female.department_name=male.department_name
),
CTE_UNDER_OVER_5(DEPARTMENT,female_over_5,MALE_OVER_5,female_under_5,MALE_under_5) as(
Select OVER_5.DEPARTMENT,OVER_5.female_over_5,OVER_5.MALE_OVER_5,COALESCE(UNDER_5.female_under_5,0),
	COALESCE(UNDER_5.MALE_under_5,0) 
	from CTE_OVER_5 OVER_5  full outer join  CTE_UNDER_5 UNDER_5
	ON OVER_5.DEPARTMENT=UNDER_5.DEPARTMENT
),
cte_finished_patients(DEPARMENT,FINISHED_PATIENTS) AS (
select dept.dpt_parent, count(reg.reg_status) as finished_patients
from registration reg join public.kmu_department dept on reg.reg_department =dept.dpt_code
join public.kmu_chart  chart on reg.reg_health_id=chart.chr_health_id 
where (reg.reg_status='*' or reg.reg_status='T' )  and to_char(reg.reg_date,'YYYY-MM') = yyyymm
group by dept.dpt_parent
),
TOTAL_CTE_FINISHEd_UNDER_OVER_5(DEPARTMENT,FEMALE,MALE,female_under_5,MALE_under_5,FINISHED_PATIENTS)
AS (
	Select TOTAL.DEPARTMENT,TOTAL.female_over_5,TOTAL.MALE_OVER_5,TOTAL.female_under_5,
	TOTAL.MALE_under_5,FINISHED.FINISHED_PATIENTS
	from CTE_UNDER_OVER_5 TOTAL  JOIN cte_finished_patients FINISHED
	ON TOTAL.DEPARTMENT=FINISHED.DEPARMENT

)

select  dept.dpt_name,FEMALE,MALE,female_under_5,MALE_under_5,FINISHED_PATIENTS
	from TOTAL_CTE_FINISHEd_UNDER_OVER_5 total ,public.kmu_department dept
	where total.DEPARTMENT=dept.dpt_code

$$;


--
-- Name: daily_sumary_by_department_f(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.daily_sumary_by_department_f() RETURNS TABLE(col1 date, col2 character varying, col3 character varying, col4 character varying)
    LANGUAGE sql
    AS $$
/*
  Date:2023/09/17
  Coder:Vivienne
  Items: Add status type: O: Observing 
*/
WITH reservationCTE(reg_date_r,dpt_name_r,patients_r)
AS
(
SELECT a.reg_date, b.dpt_name, count(a.reg_department) as patients_r
FROM public.registration a, public.kmu_department b
where (a.reg_date between (CURRENT_DATE - interval '9 day') and (CURRENT_DATE + interval '1 day'))
and a.reg_department = b.dpt_code
Group by a.reg_date, b.dpt_name
),
finishCTE(reg_date_f,dpt_name_f,patients_f)
AS
(
SELECT a.reg_date, b.dpt_name, count(a.reg_department) as patients_f
FROM public.registration a, public.kmu_department b
where (a.reg_date between (CURRENT_DATE - interval '9 day') and (CURRENT_DATE + interval '1 day'))
and a.reg_department = b.dpt_code
and (a.reg_status = '*' or a.reg_status = 'T' or a.reg_status = 'O')
Group by a.reg_date, b.dpt_name
),
left_outer_join_CTE(reg_date_r,dpt_name_r,patients_r,reg_date_f,dpt_name_f,patients_f)
AS
(
Select *
From reservationCTE
Left Outer Join finishCTE
On reservationCTE.reg_date_r = finishCTE.reg_date_f
and reservationCTE.dpt_name_r = finishCTE.dpt_name_f
)

Select reg_date_r, dpt_name_r, patients_r, Coalesce(patients_f,0)
From left_outer_join_CTE
Order by dpt_name_r, reg_date_r
$$;


--
-- Name: daily_sumary_by_department_in_detail_f(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.daily_sumary_by_department_in_detail_f() RETURNS TABLE(col1 date, col2 character varying, col3 character varying, col4 character varying, col5 character varying, col6 character varying, col7 character varying, col8 character varying)
    LANGUAGE sql
    AS $$
SELECT a.reg_date, b.dpt_name,
concat(e.user_name_firstname,' ',e.user_name_midname,' ',e.user_name_lastname) as doctor_name, a.reg_health_id,
concat(c.chr_patient_firstname,' ',c.chr_patient_midname,' ',c.chr_patient_lastname) as patient_name, c.chr_sex,
(CURRENT_DATE-c.chr_birth_date)/365 as age, d.ref_name
FROM public.registration a, public.kmu_department b, public.kmu_chart c, public.kmu_coderef d, kmu_users e
where (a.reg_date between (CURRENT_DATE - interval '9 day') and (CURRENT_DATE + interval '1 day'))
and a.reg_doctor_id = e.user_idno
and a.reg_department = b.dpt_code
and a.reg_health_id = c.chr_health_id
and (a.reg_status = d.ref_code and d.ref_name != 'Print Medical Record Style')
Order by b.dpt_name, a.reg_date, d.ref_name, a.reg_seq_no
$$;


--
-- Name: daily_sumary_f(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.daily_sumary_f() RETURNS TABLE(reg_date date, col1 character varying, col2 character varying, col3 character varying, col4 character varying, col5 character varying)
    LANGUAGE sql
    AS $$
/*
  Date:2023/09/17
  Coder:Vivienne
  Items: Add status type: O: Observing 
*/
WITH new_reg_CTE(dates,new_patients)
As
(
Select Date(modify_time) as dates, count(Date(modify_time)) as new_patients
From kmu_chart
Where (modify_time between (CURRENT_DATE - interval '9 day') and (CURRENT_DATE + interval '1 day'))
Group by Date(modify_time)
),
total_reservation_CTE(reg_date,total_patients)
As
(
Select reg_date, count(reg_date) as total_patients
From registration
Where (reg_date between (CURRENT_DATE - interval '9 day') and (CURRENT_DATE + interval '1 day'))
Group by reg_date
),
total_finish_CTE(reg_date,finish_patients)
As
(
SELECT reg_date, count(reg_date) as finish_patients 
FROM public.registration
where (reg_date between (CURRENT_DATE - interval '9 day') and (CURRENT_DATE + interval '1 day'))
and (reg_status = '*' or reg_status = 'T' or reg_status = 'O')
Group by reg_date
),
total_cancel_CTE(reg_date,cancel_patients)
As
(
SELECT reg_date, count(reg_date) as cancel_patients 
FROM public.registration
where (reg_date between (CURRENT_DATE - interval '9 day') and (CURRENT_DATE + interval '1 day'))
and reg_status = 'C'
Group by reg_date
),
total_waiting_CTE(reg_date,waiting_patients)
As
(
SELECT reg_date, count(reg_date) as waiting_patients 
FROM public.registration
where (reg_date between (CURRENT_DATE - interval '9 day') and (CURRENT_DATE + interval '1 day'))
and reg_status = 'N'
Group by reg_date
),
Join1_CTE(reg_date_r,total_patients,reg_date_f,finish_patients)
AS
(
Select *
From total_reservation_CTE
Left Outer Join total_finish_CTE
On total_finish_CTE.reg_date = total_reservation_CTE.reg_date
),
Join2_CTE(reg_date_r,total_patients,reg_date_f,finish_patients,dates,new_patients)
As
(
Select *
From Join1_CTE
Left Outer Join new_reg_CTE
On Join1_CTE.reg_date_r = new_reg_CTE.dates
),
Join3_CTE(reg_date_r,total_patients,reg_date_f,finish_patients,dates,new_patients,reg_date_c,cancel_patients)
As
(
Select *
From Join2_CTE
Left Outer Join total_cancel_CTE
On Join2_CTE.reg_date_r = total_cancel_CTE.reg_date
),
Join4_CTE(reg_date_r,total_patients,reg_date_f,finish_patients,dates,new_patients,reg_date_c,cancel_patients,reg_date_w,waiting_patients)
As
(
Select *
From Join3_CTE
Left Outer Join total_waiting_CTE
On Join3_CTE.reg_date_r = total_waiting_CTE.reg_date
)
Select reg_date_r as date, Coalesce(new_patients,0) as new_patients, total_patients, finish_patients, Coalesce(cancel_patients,0) as cancel_patients,
Coalesce(waiting_patients,0) as waiting_patients
From Join4_CTE
Order by reg_date_r
$$;


--
-- Name: fn_dailysummarybydepartment(character varying, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_dailysummarybydepartment(start_date character varying DEFAULT to_char(date_trunc('month'::text, (CURRENT_DATE)::timestamp with time zone), ('yyyy-mm-dd'::character varying)::text), end_date character varying DEFAULT to_char((CURRENT_DATE)::timestamp with time zone, ('yyyy-mm-dd'::character varying)::text)) RETURNS TABLE(dates date, department character varying, totalreserved character varying, totalfinished character varying, totalcancel character varying, totalwaiting character varying)
    LANGUAGE sql
    AS $$
/*

-- hisorderplan -- column [status] --
Values of the column [status]:the definition of values
0：Not Confirmed yet / 2: Confirmed / X：Cancelled
-- hisorderplan -- column [dc_status] --
Values of the column [dc_status]:the definition of values
0：Valid / 2：DC(Discontinue)

-- hisordersoa  -- column [status] --
Values of the column [status]:the definition of values
V：Valid / X：DC(Discontinue)
*/
/*
  Date:2023/09/17
  Coder:Vivienne
  Items: Add status type: O: Observing 
*/
/*
  Date:2023/10/12
  Coder:sahal abdi adam
  Items: change all abreviations into understandable words and changing function Names into Pascal Case
*/
/*
  Date:2023/10/18
  Coder:sahal abdi adam
  Items: add two new collumns for total Cancel And Total Waiting.
*/
/*
  Date: 2024/02/25
  Coder: Ahmed Mustapha
  Item: change into filter
*/

WITH CTE(reg_date_r,dpt_name_r,patients_r,patients_f,TotalCancel,TotalWaiting)
AS
(
select reg_date , dpt.dpt_name as department, 
       count(inhospid) as total_reservation,
	   count(inhospid) filter (where reg_status in ('*','T','O')) as total_finish,
	   count(inhospid) filter (where reg_status = 'C') AS total_cencel,
	   count(inhospid) filter (where reg_status = 'N') as total_waiting
from registration reg join kmu_department dpt on reg.reg_department = dpt.dpt_code
where to_char(reg_date, 'yyyy-mm-dd') between start_date and end_date
group by reg_date, department
)
Select reg_date_r, dpt_name_r, patients_r, Coalesce(patients_f,0) as finished_Patients,Coalesce(TotalCancel,0) as TotalCancel
,Coalesce(TotalWaiting,0) as TotalWaiting
From CTE
Order by reg_date_r, dpt_name_r 
$$;


--
-- Name: fn_dailysummarybydepartmentindetail(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_dailysummarybydepartmentindetail() RETURNS TABLE(reg_date date, dpt_name character varying, doctor_name character varying, reg_health_id character varying, patient_name character varying, chr_sex character varying, age character varying, ref_name character varying, patient_waitingtimes character varying, appointment character varying, patient_consultationduration character varying)
    LANGUAGE sql
    AS $$
/*
 28-09-2023, Vivienne,	Add patient_WaitingTimes and patient_ConsultationDuration
 11-10-2023, Vivienne, 	1.Add lfet outer join, since when making the ER registration, it's without reg_doctor_id 
 						2.Change subquery to join query with alias name
 19-10-2023, Vivienne, Add: When patients is under canncel and waiting status, no need to count waiting time
*/
SELECT reg.reg_date, dpt.dpt_name,
concat(urs.user_name_firstname,' ',urs.user_name_midname,' ',urs.user_name_lastname) as doctor_name, reg.reg_health_id,
concat(chart.chr_patient_firstname,' ',chart.chr_patient_midname,' ',chart.chr_patient_lastname) as patient_name, chart.chr_sex,
(CURRENT_DATE-chart.chr_birth_date)/365 AS age, code.ref_name, to_char(reg.reg_create_time, 'HH24:MI:SS') as appointment_time
--,reg.reg_create_time											--varifying--
--,reg.reg_start_time ,reg.reg_end_time  						--varifying--
--,reg.reg_exam_start_time,reg.reg_exam_end_time				--varifying--
--,reg.reg_observe_start_time,reg.reg_observe_end_time			--varifying--
-- When patients is under canncel and waiting status, still count waiting time--
--,to_char(reg.reg_start_time - reg.reg_create_time,'HH24:MI:SS') AS patient_WaitingTimes 	
,coalesce(to_char((CASE WHEN  reg.reg_status='C' THEN NULL WHEN  reg.reg_status='N' THEN NULL  		
		  ELSE reg.reg_start_time END)
		 - reg.reg_create_time,'HH24:MI:SS'),'') AS patient_WaitingTimes
-- When patients is under canncel and waiting status, no need to count waiting time--
,coalesce(to_char((CASE WHEN reg.reg_status='*' THEN reg.reg_end_time 
		WHEN reg.reg_status='T' AND reg.reg_exam_end_time IS null THEN reg.reg_exam_start_time 
		WHEN reg.reg_status='T' AND reg.reg_exam_end_time IS NOT null THEN reg.reg_exam_end_time 
		WHEN reg.reg_status='O' AND reg.reg_observe_end_time IS null THEN reg.reg_observe_start_time 
  		WHEN reg.reg_status='O' AND reg.reg_observe_end_time IS NOT null THEN reg.reg_observe_end_time 	
		  ELSE NULL END
 	) - (CASE WHEN reg.reg_start_time IS null THEN reg_create_time  
 		WHEN reg.reg_start_time IS NOT null THEN reg.reg_start_time END),'HH24:MI:SS'),'')
as patient_ConsultationDuration 
FROM public.registration reg 
	INNER JOIN public.kmu_department dpt on reg.reg_department= dpt.dpt_code
	INNER JOIN public.kmu_chart chart on  reg.reg_health_id=chart.chr_health_id
	INNER JOIN public.kmu_coderef code on reg.reg_status=code.ref_code AND code.ref_name != 'Print Medical Record Style'
	LEFT OUTER JOIN public.kmu_users urs on reg.reg_doctor_id=urs.user_idno
WHERE (reg.reg_date BETWEEN date_trunc('month'::text,(CURRENT_DATE)::timestamp with time zone) AND (CURRENT_DATE))
	--AND reg.reg_health_id='HG00270688' AND reg.reg_date='10-09-2023'
ORDER BY  reg.reg_date,dpt.dpt_name, code.ref_name, reg.reg_seq_no

$$;


--
-- Name: fn_dailysummarywithoutcancel(character varying, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_dailysummarywithoutcancel(start_date character varying DEFAULT to_char(date_trunc('month'::text, (CURRENT_DATE)::timestamp with time zone), 'yyyy-mm-dd'::text), end_date character varying DEFAULT to_char((CURRENT_DATE)::timestamp with time zone, 'yyyy-mm-dd'::text)) RETURNS TABLE(reg_date date, col1 character varying, col2 character varying, col3 character varying, col4 character varying)
    LANGUAGE sql
    AS $$
/*

-- hisorderplan -- column [status] --
Values of the column [status]:the definition of values
0：Not Confirmed yet / 2: Confirmed / X：Cancelled
-- hisorderplan -- column [dc_status] --
Values of the column [dc_status]:the definition of values
0：Valid / 2：DC(Discontinue)

-- hisordersoa  -- column [status] --
Values of the column [status]:the definition of values
V：Valid / X：DC(Discontinue)
*/
/*
  Date:2023/09/17
  Coder:Vivienne
  Items: Add status type: O: Observing 
*/
WITH new_reg_CTE(dates,new_patients)
As
(
Select Date(modify_time) as dates, count(Date(modify_time)) as new_patients
From kmu_chart
Where (to_char(modify_time, 'yyyy-mm-dd') between start_date and end_date)
Group by Date(modify_time)
),
total_reservation_CTE(reg_date_P,total_patients)
As
(
Select reg_date, count(reg_date) as total_patients
From registration
Where reg_status != 'C' and (to_char(reg_date, 'yyyy-mm-dd') between start_date and end_date)
Group by reg_date
),
total_finish_CTE(reg_date_F,finish_patients)
As
(
SELECT reg_date, count(reg_date) as finish_patients 
FROM public.registration
where (to_char(reg_date, 'yyyy-mm-dd') between start_date and end_date)
and (reg_status = '*' or reg_status = 'T' or reg_status = 'O')
Group by reg_date
	order by reg_date
),

--total_cancel_CTE(reg_date,cancel_patients)
--As
--(
--SELECT reg_date, count(reg_date) as cancel_patients 
--FROM public.registration
--where (reg_date between (CURRENT_DATE - interval '9 day') and (CURRENT_DATE + interval '1 day'))
--and reg_status = 'C'
--Group by reg_date
--),
total_waiting_CTE(reg_date_W,waiting_patients)
As
(
SELECT reg_date, count(reg_date) as waiting_patients 
FROM public.registration
where (to_char(reg_date, 'yyyy-mm-dd') between start_date and end_date)
and reg_status = 'N'
Group by reg_date
),
Total_FIN_Total_Resr(reg_date_p,total_patients,finish_patients,reg_date_f) as (

select * from total_reservation_CTE 
	left outer join 
	total_finish_CTE
	On total_finish_CTE.reg_date_F = total_reservation_CTE.reg_date_P
),
join_total_w_F_Resr(reg_date_p,total_patients,reg_date_f,finish_patients,reg_date_W,waiting_patients)
as (
select * from  Total_FIN_Total_Resr
	left outer join 
	total_waiting_CTE
	On Total_FIN_Total_Resr.reg_date_P= total_waiting_CTE.reg_date_W
	),
join_total_W_F_Reg_Resr(reg_date_p,total_patients,finish_patients,waiting_patients,new_patients)
as
(
select reg_date_p,coalesce(new_patients,0),coalesce(total_patients,0), coalesce(finish_patients,0),coalesce(waiting_patients,0)
	from join_total_w_F_Resr
	left outer join new_reg_CTE
	on join_total_w_F_Resr.reg_date_P=new_reg_CTE.dates
) 
select * from join_total_W_F_Reg_Resr
order by reg_date_p

$$;


--
-- Name: fn_denguefever(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_denguefever() RETURNS TABLE(reg_date date, dpt_name character varying, health_id character varying, patients_firstname character varying, patients_middlename character varying, patients_lastname character varying, plan_code character varying, plan_des character varying)
    LANGUAGE sql
    AS $$

select reg_date, dpt_name, chr_health_id, 
chr_patient_firstname, chr_patient_midname, chr_patient_lastname, plan_code, plan_des 
from registration
join hisorderplan
on hisorderplan.inhospid = registration.inhospid 
join kmu_chart
on registration.reg_health_id = kmu_chart.chr_health_id
join kmu_department
on kmu_department.dpt_code = registration.reg_department
join hisordersoa
on hisordersoa.inhospid = registration.inhospid
where plan_code in('A90', 'A91','R50') 
and reg_date = '2025-03-22' and  hplan_type = 'ICD' and kind='CM' 
order by reg_date
/*
select reg_date, dpt_name, chr_health_id, 
chr_patient_firstname, chr_patient_midname, chr_patient_lastname,chr_sex,(current_date-chr_birth_date)/365 as age, chr_area_code,chr_address,dpt_name,plan_code, plan_des 
from registration
inner join hisorderplan
on hisorderplan.inhospid = registration.inhospid 
inner join kmu_chart
on registration.reg_health_id = kmu_chart.chr_health_id
inner join kmu_department 
on kmu_department.dpt_code = registration.reg_department
inner join hisordersoa
on hisordersoa.inhospid = registration.inhospid
where plan_code in('A90', 'A91','R50') 
and reg_date between '2023-04-01' and '2023-11-30' and  hplan_type = 'ICD' and kind='CM' 
order by reg_date
*/
$$;


--
-- Name: fn_doctorattendance_monthlydetail(character varying, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_doctorattendance_monthlydetail(yyyy character varying, mm character varying) RETURNS TABLE(reg_department character varying, doctor_name text, reg_date text, daily_start_at text, daily_end_at text)
    LANGUAGE plpgsql
    AS $$
begin
  -- do some work....
   return query

	with cteAttDtl (reg_department_name,reg_doctor_name,working_date,start_working_time,end_working_time) 
	AS (
		select dept.dpt_name,concat(users.user_name_firstname,' ',users.user_name_midname,' ',users.user_name_lastname) as doctor_name,reg.reg_date
		,case when reg.reg_start_time is null then reg_create_time 
			when reg.reg_start_time is not null then reg.reg_start_time end as start_working_time
		,case when reg.reg_status='*' then reg.reg_end_time 
			when reg.reg_status='T' and reg.reg_exam_end_time is null then reg.reg_exam_start_time 
			when reg.reg_status='T' and reg.reg_exam_end_time is not null then reg.reg_exam_end_time 
			when reg.reg_status='O' and reg.reg_observe_end_time  is null then reg.reg_observe_start_time
			when reg.reg_status='O' and reg.reg_observe_end_time  is not null then reg.reg_exam_end_time
		end as end_working_time
		--,reg_start_time,reg_end_time,reg_exam_start_time,reg_exam_end_time
		--,reg.*
		from public.registration reg
		inner join public.kmu_department dept on reg.reg_department=dept.dpt_code
		inner join public.kmu_users users on reg.reg_doctor_id=users.user_idno
		where reg.reg_status in ('*','T','O') 
		and reg.reg_date between to_date(CONCAT(yyyy, '-', mm), 'YYYY-MM')::date  and (date_trunc('month', to_date(CONCAT(yyyy, '-', mm), 'YYYY-MM')::date) + interval '1 month'-  interval '1 day')::date
		--and reg.reg_date between '2023-08-23' and '2023-08-23'
		--and reg_doctor_id='CMC98'
		--and users.user_name_firstname='Jama'
		)
		select reg_department_name,reg_doctor_name,cast(working_date as text),coalesce(TO_CHAR(min(start_working_time),'HH24:MI:SS'), '00:00:00') as daily_start_at,coalesce(TO_CHAR(max(end_working_time),'HH24:MI:SS'),'00:00:00') as daily_end_at 
		from cteAttDtl group by reg_department_name,reg_doctor_name,working_date
		order by reg_department_name,reg_doctor_name,working_date;
end;
$$;


--
-- Name: fn_doctorattendance_monthlysummary(character varying, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_doctorattendance_monthlysummary(yyyy character varying, mm character varying) RETURNS TABLE(reg_department character varying, doctor_name text, working_days text, avg_arrival_time text, avg_leave_time text, avg_daily_working_hours text)
    LANGUAGE plpgsql
    AS $$
begin
   	/*
		20231014 Vivienne First Version
	*/
   return query

	with cteAttDtl (reg_department_name,reg_doctor_name,working_date,start_working_time,end_working_time) 
	AS (
		select dept.dpt_name,concat(users.user_name_firstname,' ',users.user_name_midname,' ',users.user_name_lastname) as doctor_name,reg.reg_date
		,case when reg.reg_start_time is null then reg_create_time 
			when reg.reg_start_time is not null then reg.reg_start_time end as start_working_time
		,case when reg.reg_status='*' then reg.reg_end_time 
			when reg.reg_status='T' and reg.reg_exam_end_time is null then reg.reg_exam_start_time 
			when reg.reg_status='T' and reg.reg_exam_end_time is not null then reg.reg_exam_end_time 
			when reg.reg_status='O' and reg.reg_observe_end_time  is null then reg.reg_observe_start_time
			when reg.reg_status='O' and reg.reg_observe_end_time  is not null then reg.reg_exam_end_time
		end as end_working_time
		--,reg_start_time,reg_end_time,reg_exam_start_time,reg_exam_end_time
		--,reg.*
		from public.registration reg
		inner join public.kmu_department dept on reg.reg_department=dept.dpt_code
		inner join public.kmu_users users on reg.reg_doctor_id=users.user_idno
		where reg.reg_status in ('*','T','O') 
		and reg.reg_date between to_date(CONCAT(yyyy, '-', mm), 'YYYY-MM')::date  and (date_trunc('month', to_date(CONCAT(yyyy, '-', mm), 'YYYY-MM')::date) + interval '1 month'-  interval '1 day')::date
		--and reg.reg_date between '2023-09-01' and '2023-09-30'
		--and reg_doctor_id='CMC98'
		--and users.user_name_firstname='Jama'
		),
	 cteAttSummary (reg_department_name,reg_doctor_name,working_date,min_start_working_time,max_end_working_time,daily_working_hours)
	as (
		select reg_department_name,reg_doctor_name,working_date
		,min(start_working_time) as min_start_working_time
		,max(end_working_time) as max_end_working_time
		,max(end_working_time)-min(start_working_time) as daily_working_hours
		from cteAttDtl 
		group by reg_department_name,reg_doctor_name,working_date
		order by reg_department_name,reg_doctor_name,working_date
		)
		select reg_department_name,reg_doctor_name
		,cast(count(working_date) as text )as working_days		
		,to_char(avg( min_start_working_time - min_start_working_time::date ),'HH24:MI:SS') as avg_arrival_time
		,to_char(avg( max_end_working_time- max_end_working_time::date),'HH24:MI:SS')  as avg_leave_time 
		,to_char(avg(daily_working_hours),'HH24:MI:SS') as avg_daily_working_hours
		from cteAttSummary
		group by reg_department_name,reg_doctor_name
		order by reg_department_name,reg_doctor_name;
		
end;
$$;


--
-- Name: fn_general(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_general(yyyymm character varying) RETURNS TABLE(year_ text, month_ text, new_registration character varying, total_patients text, total_finished text, finish_rate text, total_male text, total_female text, above_5 text, under_5 text)
    LANGUAGE sql
    AS $$

WITH cte_patients(year_, month_, total_patients, total_finished, total_male, total_female, above_5, under_5)
AS(
SELECT 
	  TO_CHAR(reg.reg_date, 'yyyy') as year_,
	  TO_CHAR(reg.reg_date, 'mm') as month_,
      COUNT(*) AS total_patients,
	  COUNT(*) FILTER (WHERE reg.reg_status in ('O','*','T') ) AS total_finished,
	  COUNT(*) FILTER (WHERE chr.chr_sex = 'M' AND reg.reg_status in ('O','*','T')) AS total_male,
	  COUNT(*) FILTER (WHERE chr.chr_sex = 'F' AND reg.reg_status in ('O','*','T'))AS total_female,
	  COUNT(*) FILTER (WHERE (current_date-chr.chr_birth_date)/365>5 AND reg.reg_status in ('O','*','T')) AS above_5,
	  COUNT(*) FILTER (WHERE (current_date-chr.chr_birth_date)/365<=5 AND reg.reg_status in ('O','*','T')) AS under_5
FROM registration reg LEFT JOIN kmu_chart chr on reg.reg_health_id = chr.chr_health_id
WHERE TO_CHAR(reg.reg_date, 'yyyy-mm') BETWEEN '2023-03' AND  yyyymm
GROUP BY year_, month_
),
cte_new_registration(year_, month_,new_registration)
AS(
SELECT 
	  TO_CHAR(modify_time, 'yyyy') as year_,
	  TO_CHAR(modify_time, 'mm') as month_,
      COUNT(*)  AS tota_registration
FROM kmu_chart 
WHERE TO_CHAR(modify_time, 'yyyy-mm') BETWEEN '2023-03' AND yyyymm
GROUP BY year_, month_
	
)
SELECT  c2.*, c1.total_patients, c1.total_finished, Coalesce(round((Coalesce(c1.total_finished,0.00000)/c1.total_patients), 4),0.0000) as finish_rate, c1.total_male, c1.total_female, c1.above_5, c1.under_5
FROM cte_patients c1 LEFT JOIN cte_new_registration c2 ON c1.year_ = c2.year_ and c1.month_ = c2.month_

$$;


--
-- Name: fn_monthlyreport(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_monthlyreport(yyyymm character varying) RETURNS TABLE(yyyy text, mm text, department character varying, doctor_name text, total_reservation character varying, total_finish character varying, finish_rate text, total_med_record character varying, use_icd10 character varying, not_use_icd10 character varying, icd10_usage_rate character varying, total_use_icd10 character varying, total_icd10_usage_rate character varying)
    LANGUAGE sql
    AS $$
/*
  2023/09/17 Vivienne 1. Add status type: O: Observing 
  2023/09/28 Vivienne 1. Add Total ICD10 and Total ICD10 usage rate 2. change use_icd10 definition from all icd10 to seq_no=1
  2023/10/05 Vivienne  change to left outer join  
*/
WITH CTE_total_reservation(reg_year,reg_month,dpt_parent,reg_doctor_id,doctor_name,total_reservation)
AS
(
SELECT TO_CHAR(c.reg_date , 'YYYY') AS reg_year, TO_CHAR(c.reg_date , 'MM') AS reg_month,dpt_parent,c.reg_doctor_id,
concat(b.user_name_firstname,' ',b.user_name_midname,' ',b.user_name_lastname) AS doctor_name,
COUNT(c.reg_doctor_id) AS total_reservation
FROM kmu_users b, registration c, kmu_department d
WHERE c.reg_doctor_id != 'admin'
AND TO_CHAR(c.reg_date , 'YYYY-MM') = yyyymm
AND c.reg_doctor_id = b.user_idno
AND (c.reg_department = d.dpt_code)
GROUP BY reg_doctor_id,doctor_name, d.dpt_parent, reg_year, reg_month
ORDER BY reg_doctor_id
),
CTE_total_finish(reg_year,reg_month,dpt_parent,reg_doctor_id,total_finish)
AS
(
SELECT TO_CHAR(c.reg_date , 'YYYY') AS reg_year, TO_CHAR(c.reg_date , 'MM') AS reg_month,dpt_parent,c.reg_doctor_id,
COUNT(c.reg_doctor_id) AS total_finish
FROM kmu_users b, registration c, kmu_department d
WHERE c.reg_doctor_id != 'admin'
AND TO_CHAR(c.reg_date , 'YYYY-MM') = yyyymm
AND (c.reg_status = '*' OR c.reg_status = 'T' OR c.reg_status = 'O')
AND c.reg_doctor_id = b.user_idno
AND (c.reg_department = d.dpt_code)
GROUP BY reg_doctor_id, d.dpt_parent, reg_year, reg_month
ORDER BY reg_doctor_id
),
CTE_total_med_record(reg_year,reg_month,dpt_parent, doctor_id,total_med_record)
AS
(
SELECT TO_CHAR(a.create_date , 'YYYY') as reg_year,TO_CHAR(a.create_date , 'MM') as reg_month,d.dpt_parent, a.create_user as doctor_id,
COUNT(a.create_user)/2 as total_med_record
FROM hisordersoa a, kmu_users b, registration c, kmu_department d
WHERE a.create_user != 'admin'
AND a.create_user = b.user_idno
AND TO_CHAR(a.create_date , 'YYYY-MM') = yyyymm
AND (c.reg_department = d.dpt_code and c.inhospid = a.inhospid)
GROUP BY reg_year,reg_month,d.dpt_parent,a.create_user
ORDER BY a.create_user
),
CTE_use_icd10(reg_year,reg_month,dpt_parent,doctor_id,use_icd10)
As
(
SELECT TO_CHAR(a.create_date , 'YYYY') AS reg_year,TO_CHAR(a.create_date , 'MM') AS reg_month,c.dpt_parent AS department,a.create_user AS doctor_id, count(a.inhospid) as use_icd10
FROM hisorderplan a, registration b, kmu_department c
WHERE hplan_type = 'ICD' AND dc_status ='0'
AND a.seq_no=1
AND a.create_user != 'admin'
AND a.inhospid = b.inhospid
AND b.reg_department = c.dpt_code
AND TO_CHAR(a.create_date , 'YYYY-MM') = yyyymm
GROUP BY reg_year,reg_month,department, a.create_user
ORDER BY create_user
),
CTE_total_icd10(reg_year,reg_month,dpt_parent,doctor_id,total_icd10)
AS
(
SELECT TO_CHAR(a.create_date , 'YYYY') AS reg_year,TO_CHAR(a.create_date , 'MM') AS reg_month,c.dpt_parent AS department,a.create_user AS doctor_id, count(a.inhospid) as total_icd10
FROM hisorderplan a, registration b, kmu_department c
WHERE hplan_type = 'ICD' AND dc_status ='0'
AND a.create_user != 'admin'
AND a.inhospid = b.inhospid
AND b.reg_department = c.dpt_code
AND TO_CHAR(create_date , 'YYYY-MM') = yyyymm
GROUP BY reg_year,reg_month, dpt_parent, a.create_user
ORDER BY create_user
),
CTE_total(reg_year,reg_month,dpt_name,doctor_name,total_reservation,total_finish,finish_rate,total_med_record,use_icd10,no_use_icd10,use_icd10_rate,total_icd10,total_icd10_rate)
AS
(
	SELECT tr.reg_year,tr.reg_month,dpt.dpt_name,tr.doctor_name,Coalesce(tr.total_reservation,0),Coalesce(tf.total_finish,0),round((Coalesce(tf.total_finish,0.0)/tr.total_reservation),4) AS finish_rate
			,Coalesce(tmr.total_med_record,0) AS total_med_record,Coalesce(ui.use_icd10,0) AS use_icd10 ,(Coalesce(tmr.total_med_record,0)-Coalesce(ui.use_icd10,0)) AS no_use_icd10,round((Coalesce(ui.use_icd10,0.0)/tmr.total_med_record), 4) AS use_icd10_rate
			,Coalesce(ti.total_icd10,0),round((Coalesce(ti.total_icd10,0.0)/tmr.total_med_record), 4) AS total_icd10_rate
	FROM 	CTE_total_reservation tr 
	INNER JOIN public.kmu_department dpt ON tr.dpt_parent = dpt.dpt_code
	LEFT OUTER JOIN CTE_total_finish tf ON tr.reg_year = tf.reg_year AND tr.reg_month = tf.reg_month AND tr.dpt_parent=tf.dpt_parent AND tr.reg_doctor_id=tf.reg_doctor_id
	LEFT OUTER JOIN CTE_total_med_record tmr ON tr.reg_year = tmr.reg_year AND tr.reg_month = tmr.reg_month AND tr.dpt_parent=tmr.dpt_parent AND tr.reg_doctor_id=tmr.doctor_id
    LEFT OUTER JOIN CTE_use_icd10 ui ON tr.reg_year = ui.reg_year AND tr.reg_month = ui.reg_month AND tr.dpt_parent=ui.dpt_parent AND tr.reg_doctor_id=ui.doctor_id
	LEFT OUTER JOIN CTE_total_icd10 ti ON tr.reg_year = ti.reg_year AND tr.reg_month = ti.reg_month AND tr.dpt_parent=ti.dpt_parent AND tr.reg_doctor_id=ti.doctor_id

)
SELECT reg_year,reg_month,dpt_name,doctor_name,Coalesce(total_reservation,0) AS total_reservation,Coalesce(total_finish,0) AS total_finish,Coalesce(finish_rate,0.0000) AS finish_rate,total_med_record ,Coalesce(use_icd10,0) AS use_icd10,Coalesce(no_use_icd10,0) AS no_use_icd10,Coalesce(use_icd10_rate,0.0000) AS use_icd10_rate,Coalesce(total_icd10,0) AS total_icd10,Coalesce(total_icd10_rate,0.0000) AS total_icd10_rate
FROM CTE_total
ORDER BY dpt_name,doctor_name

$$;


--
-- Name: fn_monthlyreport_er(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_monthlyreport_er(yyyymm character varying) RETURNS TABLE(yyyy text, mm text, department character varying, shift text, total_reservation character varying, total_finish character varying, total_waiting character varying, total_cancel character varying)
    LANGUAGE sql
    AS $$
/*
  2023/10/28 Vivienne 1. ER Patients under waiting
*/
with cteERShiftStatus (reg_date,reg_create_time,Shift,dpt_code,dpt_name,doctor_id,doctor_name,health_id,patient_name,chr_sex,age,status_code,status_name)
as
(
	select reg.reg_date,reg_create_time ,case when to_char(reg_create_time,'HH24:MI') between '07:30' and '13:30' then 'Shift A' 
								when to_char(reg_create_time,'HH24:MI') between '13:31' and '19:45' then 'Shift B'
								when to_char(reg_create_time,'HH24:MI') between '19:46' and '23:59' then 'Shift C' 
								when to_char(reg_create_time,'HH24:MI') between '00:00' and '07:29' then 'Shift C' 
								end as Shift
	,reg_department,dpt.dpt_name
	,reg_doctor_id,concat(urs.user_name_firstname,' ',urs.user_name_midname,' ',urs.user_name_lastname) as doctor_name
	,reg.reg_health_id,concat(cart.chr_patient_firstname,' ',cart.chr_patient_midname,' ',cart.chr_patient_lastname) as patient_name
	,cart.chr_sex,(CURRENT_DATE-cart.chr_birth_date)/365 as age
	,reg.reg_status, code.ref_name
	from public.registration reg
	inner join kmu_chart cart on reg.reg_health_id = cart.chr_health_id
	inner join public.kmu_coderef code on reg.reg_status=code.ref_code
	inner join public.kmu_department dpt on reg.reg_department=dpt.dpt_code -- and dpt.dpt_code in ('1601','1602','1603','1604')
	left outer join kmu_users urs on reg.reg_doctor_id=urs.user_idno
	 where  reg_department in ('1601','1602','1603','1604','1605')
		and to_char(reg_date,'YYYY-MM')= yyyymm
	)
select TO_CHAR(reg_date , 'YYYY') AS reg_year, TO_CHAR(reg_date , 'MM') AS reg_month
	,cte.dpt_name,cte.shift
	--,count(cte.health_id) as total
	,count(cte.health_id) filter (where cte.status_code in ('N') and Coalesce(cte.doctor_id,'')='' ) + count(cte.health_id) filter (where cte.status_code in ('C') and Coalesce(cte.doctor_id,'')='') + count(cte.health_id) filter (where cte.status_code in ('*','T','O') and Coalesce(cte.doctor_id,'')<>'') as total
	,count(cte.health_id) filter (where cte.status_code in ('*','T','O') and Coalesce(cte.doctor_id,'')<>'') as done
	,count(cte.health_id) filter (where cte.status_code in ('N') and Coalesce(cte.doctor_id,'')='' ) as waiting
	,count(cte.health_id) filter (where cte.status_code in ('C') and Coalesce(cte.doctor_id,'')='') as cancel
from cteERShiftStatus cte

group by reg_year,reg_month,cte.dpt_code,cte.dpt_name,cte.shift
order by reg_year,reg_month,cte.dpt_code,cte.dpt_name,cte.shift

$$;


--
-- Name: fn_monthlyreport_er_notfinished(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_monthlyreport_er_notfinished(yyyymm character varying) RETURNS TABLE(yyyy text, mm text, department character varying, shift text, total_reservation character varying, total_finish character varying, total_waiting character varying, total_cancel character varying)
    LANGUAGE sql
    AS $$
/*
  2023/10/28 Vivienne 1. ER Patients under waiting
*/
with cteERShiftStatus (reg_date,reg_create_time,Shift,dpt_code,dpt_name,doctor_id,doctor_name,health_id,patient_name,chr_sex,age,status_code,status_name)
as
(
	select reg.reg_date,reg_create_time ,case when to_char(reg_create_time,'HH24:MI') between '07:30' and '13:30' then 'Shift A' 
								when to_char(reg_create_time,'HH24:MI') between '13:31' and '19:45' then 'Shift B'
								when to_char(reg_create_time,'HH24:MI') between '19:46' and '23:59' then 'Shift C' 
								when to_char(reg_create_time,'HH24:MI') between '00:00' and '07:29' then 'Shift C' 
								end as Shift
	,reg_department,dpt.dpt_name
	,reg_doctor_id,concat(urs.user_name_firstname,' ',urs.user_name_midname,' ',urs.user_name_lastname) as doctor_name
	,reg.reg_health_id,concat(cart.chr_patient_firstname,' ',cart.chr_patient_midname,' ',cart.chr_patient_lastname) as patient_name
	,cart.chr_sex,(CURRENT_DATE-cart.chr_birth_date)/365 as age
	,reg.reg_status, code.ref_name
	from public.registration reg
	inner join kmu_chart cart on reg.reg_health_id = cart.chr_health_id
	inner join public.kmu_coderef code on reg.reg_status=code.ref_code
	inner join public.kmu_department dpt on reg.reg_department=dpt.dpt_code -- and dpt.dpt_code in ('1601','1602','1603','1604')
	left outer join kmu_users urs on reg.reg_doctor_id=urs.user_idno
	 where  reg_department in ('1601','1602','1603','1604','1605')
		and to_char(reg_date,'YYYY-MM')= yyyymm 
	    and reg.reg_status in ('N','C')
		and Coalesce(reg_doctor_id,'')=''
)
select TO_CHAR(reg_date , 'YYYY') AS reg_year, TO_CHAR(reg_date , 'MM') AS reg_month
	,cte.dpt_name,cte.shift
	,count(cte.health_id) as total
	,count(cte.health_id) filter (where cte.status_code in ('*','T','O')) as done
	,count(cte.health_id) filter (where cte.status_code in ('N')) as waiting
	,count(cte.health_id) filter (where cte.status_code in ('C')) as cancel
from cteERShiftStatus cte
group by reg_year,reg_month,cte.dpt_code,cte.dpt_name,cte.shift
order by reg_year,reg_month,cte.dpt_code,cte.dpt_name,cte.shift

$$;


--
-- Name: fn_monthlyreport_filter(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_monthlyreport_filter(yyyymm character varying) RETURNS TABLE(yyyy text, mm text, department character varying, doctor_name text, total_reservation character varying, total_finish character varying, finish_rate text, total_med_record character varying, use_icd10 character varying, not_use_icd10 character varying, icd10_usage_rate character varying, total_use_icd10 character varying, total_icd10_usage_rate character varying)
    LANGUAGE sql
    AS $$
/*
  2023/09/17 Vivienne 1. Add status type: O: Observing 
  2023/09/28 Vivienne 1. Add Total ICD10 and Total ICD10 usage rate 2. change use_icd10 definition from all icd10 to seq_no=1
  2023/10/05 Vivienne  change to left outer join  
  2023/10/27 Vivienne Change to filter where to reduce CTE tables
*/
WITH CTE_total_reservation(reg_year,reg_month,dpt_parent,reg_doctor_id,doctor_name,total_reservation,total_finish)
AS
(
SELECT TO_CHAR(reg.reg_date , 'YYYY') AS reg_year, TO_CHAR(reg.reg_date , 'MM') AS reg_month,dpt.dpt_parent,Coalesce(reg.reg_doctor_id,'') as reg_doctor_id ,
concat(urs.user_name_firstname,' ',urs.user_name_midname,' ',urs.user_name_lastname) AS doctor_name
	,COUNT(reg.*) AS total_reservation
	,COUNT(reg.*) filter (where reg.reg_status in ('*','T','O')) as total_finish 
FROM registration reg inner join  kmu_department dpt on reg.reg_department=dpt.dpt_code
						 full outer join kmu_users urs on reg.reg_doctor_id=urs.user_idno
WHERE  TO_CHAR(reg.reg_date , 'YYYY-MM') = yyyymm
GROUP BY reg_year,reg_month,dpt_parent,doctor_name,Coalesce(reg_doctor_id,'')
ORDER BY reg_doctor_id
),
CTE_total_med_record(reg_year,reg_month,dpt_parent, doctor_id,total_med_record)
AS
(
SELECT TO_CHAR(reg.reg_date , 'YYYY') as reg_year,TO_CHAR(reg.reg_date , 'MM') as reg_month,dpt.dpt_parent, soap.create_user as doctor_id,
COUNT(soap.create_user)/2 as total_med_record	
FROM registration reg inner join  kmu_department dpt on reg.reg_department=dpt.dpt_code
	 					inner join hisordersoa soap on reg.inhospid=soap.inhospid
						left outer join kmu_users urs on reg.reg_doctor_id=urs.user_idno		
WHERE TO_CHAR(reg.reg_date , 'YYYY-MM') = yyyymm
GROUP BY reg_year,reg_month,dpt.dpt_parent,soap.create_user
ORDER BY soap.create_user
),
CTE_use_icd10(reg_year,reg_month,dpt_parent,doctor_id,use_icd10,total_icd10)
As
(
SELECT TO_CHAR(reg.reg_date , 'YYYY') AS reg_year,TO_CHAR(reg.reg_date , 'MM') AS reg_month,dpt.dpt_parent AS department,plan.create_user AS doctor_id
	, count(plan.inhospid) filter (where plan.seq_no=1 and plan.hplan_type = 'ICD' AND plan.dc_status ='0') as use_icd10
	, count(plan.inhospid) filter (where plan.hplan_type = 'ICD' AND plan.dc_status ='0') as total_icd10
FROM registration reg inner join  kmu_department dpt on reg.reg_department=dpt.dpt_code
	 					inner join hisorderplan plan on reg.inhospid=plan.inhospid
WHERE  TO_CHAR(reg.reg_date , 'YYYY-MM') = yyyymm
GROUP BY reg_year,reg_month,department, plan.create_user
ORDER BY create_user
)
	SELECT tr.reg_year,tr.reg_month,dpt.dpt_name,tr.doctor_name
			,Coalesce(tr.total_reservation,0) as total_reservation
			,Coalesce(tr.total_finish,0) as total_finish
			,round((Coalesce(tr.total_finish,0.0)/tr.total_reservation),4) AS finish_rate
			,Coalesce(tmr.total_med_record,0) AS total_med_record,Coalesce(ui.use_icd10,0) AS use_icd10 
			,CASE WHEN Coalesce(tmr.total_med_record,0)>Coalesce(ui.use_icd10,0) 
				THEN (Coalesce(tmr.total_med_record,0)-Coalesce(ui.use_icd10,0)) ELSE 0 END AS no_use_icd10
			,Coalesce(round((Coalesce(ui.use_icd10,0.00000)/tmr.total_med_record), 4),0.0000) AS use_icd10_rate
			,Coalesce(ui.total_icd10,0) AS total_icd10
			,Coalesce(round((Coalesce(ui.total_icd10,0.0000)/tmr.total_med_record), 4),0.0000) AS total_icd10_rate
	FROM 	CTE_total_reservation tr 
	INNER JOIN public.kmu_department dpt ON tr.dpt_parent = dpt.dpt_code
	LEFT OUTER JOIN CTE_total_med_record tmr ON tr.reg_year = tmr.reg_year AND tr.reg_month = tmr.reg_month AND tr.dpt_parent=tmr.dpt_parent AND tr.reg_doctor_id=tmr.doctor_id
    LEFT OUTER JOIN CTE_use_icd10 ui ON tr.reg_year = ui.reg_year AND tr.reg_month = ui.reg_month AND tr.dpt_parent=ui.dpt_parent AND tr.reg_doctor_id=ui.doctor_id
    ORDER BY dpt_name,doctor_name

$$;


--
-- Name: fn_monthlystatistics_dailybases(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_monthlystatistics_dailybases(yyyymm character varying) RETURNS TABLE(reg_date character varying, col1 character varying, col2 character varying, col3 character varying, col4 character varying, col5 character varying)
    LANGUAGE sql
    AS $$
/*
 2023/09/17 Vivienne, Add Observing status type
 2023/10/05 Vivienne, Change to left outer join
 2023/11/02 Vivienne, Change to filter where caluse 
*/

SELECT  reg.reg_date
	,COUNT(reg.*) filter (where  chart.chr_sex='F' and (current_date-chr_birth_date)/365>5 ) AS Female_over_5
	,COUNT(reg.*) filter (where  chart.chr_sex='M' and (current_date-chr_birth_date)/365>5 ) AS Male_over_5
	,COUNT(reg.*) filter (where  chart.chr_sex='F' and (current_date-chr_birth_date)/365<=5 ) AS Female_under_5
	,COUNT(reg.*) filter (where  chart.chr_sex='M' and (current_date-chr_birth_date)/365<=5 ) AS Male_under_5
	,COUNT(reg.*) AS finished_patients
FROM registration reg INNER JOIN public.kmu_department dept ON reg.reg_department =dept.dpt_code
						INNER JOIN public.kmu_chart  chart ON reg.reg_health_id=chart.chr_health_id 
WHERE (reg.reg_status IN ('*','T','O'))  AND To_char(reg.reg_date,'YYYY-MM') = yyyymm
GROUP BY  reg.reg_date

$$;


--
-- Name: fn_monthlystatistics_departmentbases(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_monthlystatistics_departmentbases(yyyymm character varying) RETURNS TABLE(department character varying, col1 character varying, col2 character varying, col3 character varying, col4 character varying, col5 character varying)
    LANGUAGE sql
    AS $$
/*
  2023/09/17 Vivienne, Add status type: O: Observing, and status type : T 
  2023/11/02 Vivienne, Change to filter where clause
*/
WITH 
cte_finished_patients(dpt_parent,Female_over_5,Male_over_5,Female_under_5,Male_under_5,finished_patients) AS (
SELECT dept.dpt_parent
	,COUNT(reg.*) filter (where  chart.chr_sex='F' and (current_date-chr_birth_date)/365>5 ) AS Female_over_5
	,COUNT(reg.*) filter (where  chart.chr_sex='M' and (current_date-chr_birth_date)/365>5 ) AS Male_over_5
	,COUNT(reg.*) filter (where  chart.chr_sex='F' and (current_date-chr_birth_date)/365<=5 ) AS Female_under_5
	,COUNT(reg.*) filter (where  chart.chr_sex='M' and (current_date-chr_birth_date)/365<=5 ) AS Male_under_5
	,COUNT(reg.*) AS finished_patients
FROM registration reg INNER JOIN public.kmu_department dept ON reg.reg_department =dept.dpt_code
						INNER JOIN public.kmu_chart  chart ON reg.reg_health_id=chart.chr_health_id 
WHERE (reg.reg_status IN ('*','T','O'))  AND To_char(reg.reg_date,'YYYY-MM') = yyyymm
GROUP BY dept.dpt_parent
)
SELECT  dept.dpt_name,cte.Female_over_5,cte.Male_over_5,cte.Female_under_5,cte.Male_under_5,cte.finished_patients
FROM cte_finished_patients cte INNER JOIN  public.kmu_department dept ON CTE.dpt_parent=dept.dpt_code

$$;


--
-- Name: fn_totalsumary(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_totalsumary(end_date character varying DEFAULT to_char((date_trunc('month'::text, (CURRENT_DATE)::timestamp with time zone) - '1 day'::interval), 'yyyy-mm-dd'::text)) RETURNS TABLE(col1 character varying, col2 character varying, col3 character varying, col4 character varying, col5 character varying, col6 character varying)
    LANGUAGE sql
    AS $$
/*

-- hisorderplan -- column [status] --
Values of the column [status]:the definition of values
0：Not Confirmed yet / 2: Confirmed / X：Cancelled
-- hisorderplan -- column [dc_status] --
Values of the column [dc_status]:the definition of values
0：Valid / 2：DC(Discontinue)

-- hisordersoa  -- column [status] --
Values of the column [status]:the definition of values
V：Valid / X：DC(Discontinue)
*/
/*
  Date:2023/09/17
  Coder:Vivienne
  Items: Add status type: O: Observing
*/
WITH new_reg_CTE(new_registration)
As
(
Select count(chr_health_id)
From kmu_chart
),
total_reservation_CTE(total_reservation)
As
(
Select count(inhospid)
From registration where reg_status != 'C'
),
total_finish_CTE(total_finish)
As
(
Select count(inhospid)
From registration 
Where (reg_status = '*' or reg_status = 'T' or reg_status = 'O')
),
total_cancel_CTE(total_cancel)
As
(
Select count(inhospid)
From registration 
Where reg_status = 'C'
),
total_waiting_CTE(total_waiting)
As
(
Select count(inhospid)
From registration 
Where reg_status = 'N'
),
total_history_finished(total_patient)
as 
(
Select count(inhospid)
From registration where to_char(reg_date, 'yyyy-mm-dd') 
between '2000-01-01' and end_date
and reg_status IN ('*','T','O')
)
Select *
From new_reg_CTE, total_reservation_CTE, total_finish_CTE, total_cancel_CTE, total_waiting_CTE,total_history_finished
$$;


--
-- Name: frequently_used_icd10_f(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.frequently_used_icd10_f(yyyymm character varying) RETURNS TABLE(dpt_name character varying, icd_code character varying, icd_english_name character varying, amount character varying)
    LANGUAGE sql
    AS $$
WITH CTE(dpt_parent,icd_code,icd_english_name,icd10_statistics)
As
(
Select d.dpt_parent, c.icd_code, c.icd_english_name, count(c.icd_code) as icd10_statistics
From registration a,hisorderplan b,kmu_icd c,kmu_department d
Where TO_CHAR(a.reg_date , 'YYYY-MM') = '2025-01'
and a.inhospid = b.inhospid
and (c.icd_code = b.plan_code and b.plan_code != '1111') 
and b.hplan_type = 'ICD' and b.dc_status = '0'
and a.reg_department = d.dpt_code
Group by d.dpt_parent, c.icd_code, c.icd_english_name
Order by d.dpt_parent, icd10_statistics DESC, c.icd_code
)
Select a.dpt_name, b.icd_code, b.icd_english_name, b.icd10_statistics::character varying AS amount
From kmu_department a, CTE b
Where a.dpt_code = b.dpt_parent
  And a.dpt_name IN ('MHD Room 2', 'MHD Room 3', 'MHD Room 4') -- Target rooms filter added here
Order by a.dpt_name, b.icd10_statistics DESC, b.icd_code
$$;


--
-- Name: monthly_doctorattendance_f(character varying, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.monthly_doctorattendance_f(yyyy character varying, mm character varying) RETURNS TABLE(reg_department character varying, doctor_name text, reg_date date, daily_start_at text, daily_end_at text)
    LANGUAGE plpgsql
    AS $$
begin
  -- do some work....
   return query

	with cteAttDtl (reg_department_name,reg_doctor_name,working_date,start_working_time,end_working_time) 
	AS (
		select dept.dpt_name,concat(users.user_name_firstname,' ',users.user_name_midname,' ',users.user_name_lastname) as doctor_name,reg.reg_date
		,case when reg.reg_start_time is null then reg_create_time 
			when reg.reg_start_time is not null then reg.reg_start_time end as start_working_time
		,case when reg.reg_status='*' then reg.reg_end_time 
			when reg.reg_status='T' and reg.reg_exam_end_time is null then reg.reg_exam_start_time 
			when reg.reg_status='T' and reg.reg_exam_end_time is not null then reg.reg_exam_end_time 
			when reg.reg_status='O' then reg.reg_exam_start_time end as end_working_time
		--,reg_start_time,reg_end_time,reg_exam_start_time,reg_exam_end_time
		--,reg.*
		from public.registration reg
		inner join public.kmu_department dept on reg.reg_department=dept.dpt_code
		inner join public.kmu_users users on reg.reg_doctor_id=users.user_idno
		where reg.reg_status in ('*','T','O') 
		and reg.reg_date between to_date(CONCAT(yyyy, '-', mm), 'YYYY-MM')::date  and (date_trunc('month', to_date(CONCAT(yyyy, '-', mm), 'YYYY-MM')::date) + interval '1 month'-  interval '1 day')::date
		--and reg.reg_date between '2023-08-23' and '2023-08-23'
		--and reg_doctor_id='CMC98'
		--and users.user_name_firstname='Jama'
		)
		select reg_department_name,reg_doctor_name,working_date,TO_CHAR(min(start_working_time),'HH24:MI:SS') as daily_start_at,TO_CHAR(max(end_working_time),'HH24:MI:SS') as daily_end_at 
		from cteAttDtl group by reg_department_name,reg_doctor_name,working_date
		order by reg_department_name,reg_doctor_name,working_date;
end;
$$;


--
-- Name: monthly_med_others_f(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.monthly_med_others_f(yyyymm character varying) RETURNS TABLE(med_name character varying, amount character varying)
    LANGUAGE sql
    AS $$
select lower(trim(remark)) as new_drugs, count(lower(trim(remark))) as total from hisorderplan 
where TO_CHAR(create_date, 'YYYY-MM') = yyyymm
and hplan_type = 'Med' and dc_status = '0' and plan_code ='405'
Group by lower(trim(remark))
Order by total DESC, lower(trim(remark))
$$;


--
-- Name: monthly_report_f(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.monthly_report_f(yyyymm character varying) RETURNS TABLE(yyyy text, mm text, department character varying, doctor_name text, total_reservation character varying, total_finish character varying, finish_rate text, total_med_record character varying, use_icd10 character varying, not_use_icd10 character varying, icd10_usage_rate character varying)
    LANGUAGE sql
    AS $$
/*
  Date:2023/09/17
  Coder:Vivienne
  Items: Add status type: O: Observing 
*/
WITH CTE_1(reg_year,reg_month,department,doctor_name,total_reservation)
As
(
Select TO_CHAR(c.reg_date , 'YYYY') as reg_year, TO_CHAR(c.reg_date , 'MM') as reg_month,dpt_parent,
concat(b.user_name_firstname,' ',b.user_name_midname,' ',b.user_name_lastname) as doctor_name,
count(c.reg_doctor_id) as total_reservation
From kmu_users b, registration c, kmu_department d
Where c.reg_doctor_id != 'admin'
and TO_CHAR(c.reg_date , 'YYYY-MM') = yyyymm
and c.reg_doctor_id = b.user_idno
and (c.reg_department = d.dpt_code)
Group by doctor_name, d.dpt_parent, reg_year, reg_month
Order by d.dpt_parent, doctor_name
),
CTE_2(reg_year,reg_month,department,doctor_name,total_finish)
As
(
Select TO_CHAR(c.reg_date , 'YYYY') as reg_year, TO_CHAR(c.reg_date , 'MM') as reg_month,dpt_parent,
concat(b.user_name_firstname,' ',b.user_name_midname,' ',b.user_name_lastname) as doctor_name,
count(c.reg_doctor_id) as total_reservation
From kmu_users b, registration c, kmu_department d
Where c.reg_doctor_id != 'admin'
and TO_CHAR(c.reg_date , 'YYYY-MM') = yyyymm
and (c.reg_status = '*' or c.reg_status = 'T' or c.reg_status = 'O')
and c.reg_doctor_id = b.user_idno
and (c.reg_department = d.dpt_code)
Group by doctor_name, d.dpt_parent, reg_year, reg_month
Order by d.dpt_parent, doctor_name
),
CTE_3(col0,col1,col2,col3,col4,col5,col6,col7,col8,col9)
As
(
Select *
From CTE_1
Left Outer Join CTE_2
on CTE_1.reg_year = CTE_2.reg_year and CTE_1.reg_month = CTE_2.reg_month
and CTE_1.department = CTE_2.department and CTE_1.doctor_name = CTE_2.doctor_name
Order by CTE_1.department, CTE_1.doctor_name
),
CTE_4(_year,_month,dpt,doctor,total,finish,finish_rate)
As
(
Select a.col0, a.col1, b.dpt_name as department, a.col3, 
a.col4 as total_reservation, Coalesce(a.col9,0) as total_finish,
round((Coalesce(a.col9,0.0)/a.col4),4) as finish_rate
From CTE_3 a,  kmu_department b
Where a.col2 = b.dpt_code
Order by department,finish_rate DESC
),
CTE_5(department, doctor_id,doctor_name,total_med_record)
As
(
Select d.dpt_parent, a.create_user as doctor_id,
concat(b.user_name_firstname,' ',b.user_name_midname,' ',b.user_name_lastname) as doctor_name,
count(a.create_user)/2 as total_med_record
From hisordersoa a, kmu_users b, registration c, kmu_department d
Where a.create_user != 'admin'
and a.create_user = b.user_idno
and TO_CHAR(a.create_date , 'YYYY-MM') = yyyymm
and (c.reg_department = d.dpt_code and c.inhospid = a.inhospid)
Group by a.create_user, doctor_name, d.dpt_parent
Order by a.create_user
),
CTE_6(department,doctor_id,inhospid,use_icd10)
As
(
Select c.dpt_parent as department,a.create_user as doctor_id, a.inhospid, count(a.inhospid) as use_icd10
From hisorderplan a, registration b, kmu_department c
Where hplan_type = 'ICD' and dc_status ='0'
and a.create_user != 'admin'
and a.inhospid = b.inhospid
and b.reg_department = c.dpt_code
and TO_CHAR(create_date , 'YYYY-MM') = yyyymm
Group by department, a.create_user, a.inhospid
Order by create_user, inhospid
),
CTE_7(department,doctor_id,use_icd10)
As
(
Select department, doctor_id, count(doctor_id) as use_icd10
From CTE_6
Group by department, doctor_id
Order by doctor_id
),
CTE_8(department,doctor_id,doctor_name,total_med_record,department_2,doctor_id_2,use_icd10)
As
(
Select *
From CTE_5
Left Outer Join CTE_7
on CTE_5.doctor_id = CTE_7.doctor_id and CTE_5.department = CTE_7.department
Order by CTE_5.doctor_id
),
CTE_9(dpt,doctor,total_med,use_icd10,no_use_icd10,icd10_usage_rate)
As
(
Select b.dpt_name, a.doctor_name, a.total_med_record, Coalesce(a.use_icd10,0) as use_icd10,
(a.total_med_record-Coalesce(a.use_icd10,0)) as no_use_icd10,
round((Coalesce(a.use_icd10,0.0)/a.total_med_record), 4) as icd10_usage_rate
From CTE_8 a,  kmu_department b
Where a.department = b.dpt_code
Order by department,icd10_usage_rate DESC,a.doctor_id
),
CTE_10(col0,col1,col2,col3,col4,col5,col6,col7,col8,col9,col10,col11,col12)
AS
(
select *
from CTE_4
Left Outer Join CTE_9
On CTE_4.doctor = CTE_9.doctor and CTE_4.dpt = CTE_9.dpt
)
select col0, col1, col2, col3, col4, col5, Coalesce(col6,'0.0000'), Coalesce(col9,0), Coalesce(col10,0), Coalesce(col11,0), Coalesce(col12,'0.0000')
from CTE_10
Order by col2, col6 DESC, col12 DESC
$$;


--
-- Name: sp_getserialno(character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_getserialno(IN inserialowner character varying, OUT outserialno character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
 vSerialNo kmu_serialpool.serial_no%Type;
 vSerialMaxNo kmu_serialpool.serial_maxno%Type;
 vSerialPrefix kmu_serialpool.serial_prefix%Type;
 vExist integer;
 vPrefixLen integer;
 vMaxLen	integer;
 vFormatChar character varying;
 pool_cur CURSOR FOR
 	SELECT serial_no,serial_maxno,COALESCE(serial_prefix,'') FROM kmu_serialpool
	WHERE serial_owner = inSerialOwner;
BEGIN 
	
	SELECT count(*) INTO vExist FROM kmu_serialpool    
	WHERE serial_owner = inSerialOwner;
		
	IF vExist <> 0
	THEN
		OPEN pool_cur;
		FETCH pool_cur INTO vSerialNo,vSerialMaxNo,vSerialPrefix;
		CLOSE pool_cur;
		
	UPDATE kmu_serialpool SET serial_no =vSerialNo+1
	WHERE serial_owner = inSerialOwner;

	END IF;
		
	vMaxLen := LENGTH(TRIM(vSerialMaxNo));
	vPrefixLen :=LENGTH(TRIM(vSerialPrefix));
	
	vFormatChar :='';
	FOR vLoopIndex IN 1..vMaxLen-1
	LOOP
		vFormatChar := vFormatChar ||'0';
	END LOOP;
	
	IF vSerialPrefix <> '' THEN
	outSerialNo := vSerialPrefix || TO_CHAR(vSerialNo,'FM'||vFormatChar);
	ELSE
	outSerialNo := TO_CHAR(vSerialNo,'FM'||vFormatChar);
	END IF;
	
	--outSerialNo := vSerialPrefix || TO_CHAR(vSerialNo,'FM'||vFormatChar);
	
	--outSerialNo := vFormatChar;
	--outSerialNo := vSerialPrefix || vFormatChar;
	
	
END
$$;


--
-- Name: total_sumary_f(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.total_sumary_f() RETURNS TABLE(col1 character varying, col2 character varying, col3 character varying, col4 character varying, col5 character varying)
    LANGUAGE sql
    AS $$
/*
  Date:2023/09/17
  Coder:Vivienne
  Items: Add status type: O: Observing
*/
WITH new_reg_CTE(new_registration)
As
(
Select count(chr_health_id)
From kmu_chart
),
total_reservation_CTE(total_reservation)
As
(
Select count(inhospid)
From registration
),
total_finish_CTE(total_finish)
As
(
Select count(inhospid)
From registration 
Where (reg_status = '*' or reg_status = 'T' or reg_status = 'O')
),
total_cancel_CTE(total_cancel)
As
(
Select count(inhospid)
From registration 
Where reg_status = 'C'
),
total_waiting_CTE(total_waiting)
As
(
Select count(inhospid)
From registration 
Where reg_status = 'N'
)
Select *
From new_reg_CTE, total_reservation_CTE, total_finish_CTE, total_cancel_CTE, total_waiting_CTE
$$;


--
-- Name: tr_bedid(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.tr_bedid() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
	bedId beds."bedid"%TYPE;
BEGIN	
	IF NEW."bedid" IS null OR NEW."bedid" = ''
	THEN
	call sp_getserialno('BedId',bedId);
	
	NEW."bedid" := bedId;
	END IF;
	RETURN NEW;
END;
$$;


--
-- Name: tr_calllog(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.tr_calllog() RETURNS trigger
    LANGUAGE plpgsql
    AS $$BEGIN  
  IF NEW.call_time is not null THEN
     INSERT INTO public.trans_call_log(
	     log_date, call_id
		 , call_reg_date, call_reg_department
		 , call_reg_noon, call_reg_seq_no
		 , call_patient_id, inhospid
		 , call_time, modify_user
		 , modify_time)
	VALUES (now(),NEW.call_id
			, NEW.call_reg_date, NEW.call_reg_department
			, NEW.call_reg_noon, NEW.call_reg_seq_no
			, NEW.call_patient_id, NEW.inhospid
			, NEW.call_time,NEW.modify_user
			, NEW.modify_time);
  END IF;
  RETURN NEW;		
END; 
$$;


--
-- Name: tr_coderef(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.tr_coderef() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
	ref_id kmu_coderef.ref_id%TYPE;
BEGIN	
	IF NEW.ref_id IS null OR NEW.ref_id = ''
	THEN
	call sp_getserialno('CodeRef',ref_id);
	
	NEW.ref_id := ref_id;
	END IF;
	RETURN NEW;
END;
$$;


--
-- Name: tr_hisorderplan(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.tr_hisorderplan() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
	hplanid hisorderplan.orderplanid%TYPE;
BEGIN	
	IF NEW.orderplanid IS null OR NEW.orderplanid =-1
	THEN
	NEW.orderplanid :=  nextval('hisorderplan_orderplanid_seq'::regclass);
	END IF;
	RETURN NEW;
END;
$$;


--
-- Name: tr_hisordersoa(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.tr_hisordersoa() RETURNS trigger
    LANGUAGE plpgsql
    AS $$DECLARE
	soaid hisordersoa.soaid%TYPE;
BEGIN	
	IF NEW.soaid IS null OR NEW.soaid =-1
	THEN
	NEW.soaid :=  nextval('hisorderplan_soa_soaid_seq'::regclass);
	END IF;
	RETURN NEW;
END;

$$;


--
-- Name: tr_inhospid(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.tr_inhospid() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
	inhospid registration.inhospid%TYPE;
BEGIN	
	IF NEW.inhospid IS null OR NEW.inhospid =''
	THEN
	call sp_getserialno('InHospID',inhospid);
	
	NEW.inhospid := inhospid;
	END IF;
	RETURN NEW;
END;
$$;


--
-- Name: tr_inpatientid(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.tr_inpatientid() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
	inhospid inptient_reservation.inhospid%TYPE;
BEGIN	
	IF NEW.inhospid IS null OR NEW.inhospid =''
	THEN
	call sp_getserialno('InpatientId',inhospid);
	
	NEW.inhospid := inhospid;
	END IF;
	RETURN NEW;
END;
$$;


--
-- Name: tr_kmu_upload_log(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.tr_kmu_upload_log() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
	logid kmu_upload_log.logid%TYPE;
BEGIN	
	IF NEW.logid IS null OR NEW.logid =-1
	THEN
	NEW.logid :=  nextval('kmu_upload_log_logid_seq'::regclass);
	END IF;
	RETURN NEW;
END;
$$;


--
-- Name: tr_kmuchartlogid(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.tr_kmuchartlogid() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
log_id kmu_chart_log.log_id%TYPE;
BEGIN
	IF NEW.log_id IS null OR NEW.log_id =''
	THEN
	call sp_getserialno('KmuChartLog',log_id);
	NEW.log_id := log_id;
	END IF;
	RETURN NEW;
END;
$$;


--
-- Name: tr_physical(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.tr_physical() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
	phyID physical_sign.phy_id%TYPE;
BEGIN	
	IF NEW.phy_id IS null OR NEW.phy_id = ''
	THEN
	call sp_getserialno('PhysicalSign',phyID);
	
	NEW.phy_id := phyID;
	END IF;
	RETURN NEW;
END;
$$;


--
-- Name: tr_registertocall(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.tr_registertocall() RETURNS trigger
    LANGUAGE plpgsql
    AS $$BEGIN  
  if new.reg_call_time is not null then
  INSERT INTO public.trans_call(
	call_id, call_reg_date, call_reg_department, call_reg_noon, call_reg_seq_no, call_patient_id, inhospid, call_time, modify_user, modify_time)
	VALUES (1, new.reg_date, new.reg_department, new.reg_noon, new.reg_seq_no
		, new.reg_patient_id, new.inhospid , new.reg_call_time, 'admin',now());
  end if;
  return new;		
END; $$;


--
-- Name: tr_wardid(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.tr_wardid() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
	wardid wards."wardid"%TYPE;
BEGIN	
	IF NEW.wardid IS null OR NEW.wardid = ''
	THEN
	call sp_getserialno('WardId',wardid);
	
	NEW.wardid := wardid;
	END IF;
	RETURN NEW;
END;
$$;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: AspNetUsers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AspNetUsers" (
    "Id" text NOT NULL,
    "UserName" character varying(256),
    "NormalizedUserName" character varying(256),
    "Email" character varying(256),
    "NormalizedEmail" character varying(256),
    "EmailConfirmed" boolean NOT NULL,
    "PasswordHash" text,
    "SecurityStamp" text,
    "ConcurrencyStamp" text,
    "PhoneNumber" text,
    "PhoneNumberConfirmed" boolean NOT NULL,
    "TwoFactorEnabled" boolean NOT NULL,
    "LockoutEnd" timestamp with time zone,
    "LockoutEnabled" boolean NOT NULL,
    "AccessFailedCount" integer NOT NULL,
    "FirstName" character varying(50) DEFAULT ''::character varying NOT NULL,
    "MiddleName" character varying(50) DEFAULT ''::character varying,
    "LastName" character varying(50) DEFAULT ''::character varying NOT NULL,
    "Location" character varying(200) DEFAULT ''::character varying
);


--
-- Name: BloodUnits; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."BloodUnits" (
    id text NOT NULL,
    "IntakeTag" text DEFAULT ''::text NOT NULL,
    "DonorFirstName" text DEFAULT ''::text NOT NULL,
    "DonorMiddleName" text DEFAULT ''::text NOT NULL,
    "DonorLastName" text DEFAULT ''::text NOT NULL,
    "DonorName" text DEFAULT ''::text NOT NULL,
    "Village" text DEFAULT ''::text NOT NULL,
    "Phone" text DEFAULT ''::text NOT NULL,
    "BloodType" text DEFAULT ''::text NOT NULL,
    "Component" text DEFAULT ''::text NOT NULL,
    "Purpose" text DEFAULT ''::text NOT NULL,
    "PatientName" text,
    "PatientWard" text,
    "RequesterName" text,
    "RequesterSection" text,
    "Status" text DEFAULT 'Screening'::text NOT NULL,
    "BagSerial" text,
    "Reason" text,
    "ScreenedBy" text,
    "ScreenedAt" timestamp without time zone,
    "CreatedAt" timestamp without time zone DEFAULT (now() AT TIME ZONE 'utc'::text) NOT NULL,
    "UpdatedAt" timestamp without time zone,
    "RegisteredBy" text DEFAULT ''::text NOT NULL
);


--
-- Name: dhis2_diseases; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.dhis2_diseases (
    dhis2_code integer NOT NULL,
    diseases character varying(400) NOT NULL,
    show_seq integer
);


--
-- Name: DHIS2DISEASES_DHIS2_CODE_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.dhis2_diseases ALTER COLUMN dhis2_code ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."DHIS2DISEASES_DHIS2_CODE_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: DonorProfiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."DonorProfiles" (
    "DonorId" integer NOT NULL,
    "UserId" text NOT NULL,
    "BloodGroup" integer NOT NULL,
    "LastDonationDate" timestamp without time zone,
    "EligibilityStatus" boolean DEFAULT false NOT NULL,
    "Location" character varying(200) DEFAULT ''::character varying NOT NULL
);


--
-- Name: DonorProfiles_DonorId_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."DonorProfiles" ALTER COLUMN "DonorId" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."DonorProfiles_DonorId_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: KMU_MergeHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."KMU_MergeHistory" (
    "Id" integer NOT NULL,
    "InhospId" text,
    chr_halth_id text,
    mh_health_id text,
    merged_time timestamp without time zone NOT NULL
);


--
-- Name: KMU_MergeHistory_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."KMU_MergeHistory" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."KMU_MergeHistory_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: LedgerActions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."LedgerActions" (
    "ActionId" integer NOT NULL,
    "UnitId" text NOT NULL,
    "ActionType" character varying(100) NOT NULL,
    "PerformedByUserId" text NOT NULL,
    "Timestamp" timestamp without time zone DEFAULT (now() AT TIME ZONE 'utc'::text) NOT NULL,
    "Notes" character varying(500) DEFAULT ''::character varying NOT NULL
);


--
-- Name: LedgerActions_ActionId_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."LedgerActions" ALTER COLUMN "ActionId" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."LedgerActions_ActionId_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: PatientRequests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."PatientRequests" (
    "RequestId" integer NOT NULL,
    "HospitalName" character varying(150) NOT NULL,
    "Location" character varying(200) DEFAULT ''::character varying NOT NULL,
    "RequestedBloodGroup" integer NOT NULL,
    "RequestedComponent" integer NOT NULL,
    "UnitsRequired" integer NOT NULL,
    "Priority" integer NOT NULL,
    "Status" integer NOT NULL,
    "RequestDate" timestamp without time zone DEFAULT (now() AT TIME ZONE 'utc'::text) NOT NULL
);


--
-- Name: PatientRequests_RequestId_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."PatientRequests" ALTER COLUMN "RequestId" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."PatientRequests_RequestId_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: beds; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.beds (
    bedid character varying(50) NOT NULL,
    bed_name text,
    ward_id text,
    status text,
    create_by character varying(7),
    create_at timestamp without time zone DEFAULT now() NOT NULL,
    modify_by character varying(7),
    modify_at timestamp without time zone DEFAULT now()
);


--
-- Name: COLUMN beds.bed_name; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.beds.bed_name IS 'name of the bed';


--
-- Name: blood_audit_logs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.blood_audit_logs (
    id character varying(50) NOT NULL,
    entity_name character varying(100) NOT NULL,
    entity_id character varying(50) NOT NULL,
    action character varying(50) NOT NULL,
    before_snapshot text,
    after_snapshot text,
    performed_by character varying(50) NOT NULL,
    performed_at timestamp without time zone DEFAULT now() NOT NULL
);


--
-- Name: blood_bank_blood_unit; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.blood_bank_blood_unit (
    unit_id bigint NOT NULL,
    serial_number character varying(30) NOT NULL,
    donor_id bigint NOT NULL,
    blood_type character varying(5) NOT NULL,
    component_type character varying(50) NOT NULL,
    status character varying(20) NOT NULL,
    quantity integer NOT NULL,
    donation_date timestamp without time zone NOT NULL,
    expiry_date timestamp without time zone NOT NULL,
    screening_result character varying(20) NOT NULL,
    patient_id character varying(30),
    request_id bigint,
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL,
    modify_user character varying(7),
    modify_date timestamp without time zone,
    volume_ml integer,
    storage_location character varying(100)
);


--
-- Name: blood_bank_blood_unit_unit_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.blood_bank_blood_unit_unit_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: blood_bank_blood_unit_unit_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.blood_bank_blood_unit_unit_id_seq OWNED BY public.blood_bank_blood_unit.unit_id;


--
-- Name: blood_bank_dispense; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.blood_bank_dispense (
    dispense_id bigint NOT NULL,
    unit_id bigint NOT NULL,
    request_id bigint,
    patient_id character varying(30) NOT NULL,
    inhospid character varying(30) NOT NULL,
    patient_name character varying(200) NOT NULL,
    collector_name character varying(100) NOT NULL,
    ward_destination character varying(100) NOT NULL,
    units_released integer NOT NULL,
    serial_numbers character varying(500) NOT NULL,
    dispense_date_time timestamp without time zone NOT NULL,
    dispensing_staff_id character varying(7) NOT NULL,
    dispensing_staff_name character varying(100) NOT NULL,
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL,
    source_donor_id bigint,
    donation_source character varying(100)
);


--
-- Name: blood_bank_dispense_dispense_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.blood_bank_dispense_dispense_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: blood_bank_dispense_dispense_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.blood_bank_dispense_dispense_id_seq OWNED BY public.blood_bank_dispense.dispense_id;


--
-- Name: blood_bank_donor; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.blood_bank_donor (
    donor_id bigint NOT NULL,
    first_name character varying(50) NOT NULL,
    last_name character varying(50) NOT NULL,
    gender character varying(10) NOT NULL,
    phone character varying(20) NOT NULL,
    blood_type character varying(5) NOT NULL,
    donation_type character varying(20) NOT NULL,
    patient_id character varying(30),
    inhospid character varying(30),
    request_id bigint,
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL,
    modify_user character varying(7),
    modify_date timestamp without time zone,
    status character varying(20) DEFAULT 'Registered'::character varying NOT NULL,
    screening_result character varying(20) DEFAULT 'Pending'::character varying NOT NULL,
    screening_notes character varying(500),
    bloodbankdonordonorid bigint,
    middle_name character varying(50),
    age integer,
    job_description character varying(100),
    pre_donation_malaria_result character varying(10),
    national_id character varying(100),
    email character varying(255),
    address character varying(500),
    donor_notes character varying(500),
    last_donation_date timestamp without time zone
);


--
-- Name: blood_bank_donor_donor_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.blood_bank_donor_donor_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: blood_bank_donor_donor_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.blood_bank_donor_donor_id_seq OWNED BY public.blood_bank_donor.donor_id;


--
-- Name: blood_bank_patient_event; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.blood_bank_patient_event (
    event_id bigint NOT NULL,
    patient_id character varying(30) NOT NULL,
    inhospid character varying(30) NOT NULL,
    event_type character varying(50) NOT NULL,
    event_description text NOT NULL,
    event_date_time timestamp without time zone NOT NULL,
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL
);


--
-- Name: blood_bank_patient_event_event_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.blood_bank_patient_event_event_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: blood_bank_patient_event_event_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.blood_bank_patient_event_event_id_seq OWNED BY public.blood_bank_patient_event.event_id;


--
-- Name: blood_bank_request; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.blood_bank_request (
    request_id bigint NOT NULL,
    orderplanid bigint,
    patient_id character varying(30) NOT NULL,
    inhospid character varying(30) NOT NULL,
    patient_name character varying(200) NOT NULL,
    ward character varying(50),
    bed_location character varying(50),
    requesting_doctor_id character varying(20) NOT NULL,
    requesting_doctor_name character varying(100) NOT NULL,
    blood_type character varying(5) NOT NULL,
    component_type character varying(50) NOT NULL,
    units_requested integer NOT NULL,
    urgency_level character varying(20) NOT NULL,
    patient_type character varying(10) NOT NULL,
    status character varying(20) NOT NULL,
    request_date_time timestamp without time zone NOT NULL,
    reject_reason character varying(500),
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL,
    modify_user character varying(7),
    modify_date timestamp without time zone
);


--
-- Name: blood_bank_request_request_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.blood_bank_request_request_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: blood_bank_request_request_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.blood_bank_request_request_id_seq OWNED BY public.blood_bank_request.request_id;


--
-- Name: blood_bank_screening; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.blood_bank_screening (
    screening_id bigint NOT NULL,
    unit_id bigint,
    staff_user_id character varying(7) NOT NULL,
    staff_name character varying(100) NOT NULL,
    screening_date_time timestamp without time zone NOT NULL,
    hiv_result character varying(20) NOT NULL,
    hep_b_result character varying(20) NOT NULL,
    hep_c_result character varying(20) NOT NULL,
    syphilis_result character varying(20) NOT NULL,
    malaria_result character varying(20) NOT NULL,
    overall_result character varying(20) NOT NULL,
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL,
    donor_id bigint,
    notes character varying(500)
);


--
-- Name: blood_bank_screening_screening_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.blood_bank_screening_screening_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: blood_bank_screening_screening_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.blood_bank_screening_screening_id_seq OWNED BY public.blood_bank_screening.screening_id;


--
-- Name: blood_cbc_results; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.blood_cbc_results (
    id character varying(50) NOT NULL,
    blood_unit_id character varying(50) NOT NULL,
    hemoglobin numeric(10,2),
    wbc numeric(10,2),
    platelets numeric(10,2),
    hematocrit numeric(10,2),
    decision character varying(20) NOT NULL,
    note text,
    recorded_by character varying(50) NOT NULL,
    recorded_at timestamp without time zone DEFAULT now() NOT NULL
);


--
-- Name: blood_doctor_requests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.blood_doctor_requests (
    id character varying(50) NOT NULL,
    health_id character varying(10),
    inhosp_id character varying(20),
    doctor_id character varying(7) NOT NULL,
    ward_id integer,
    component character varying(50) NOT NULL,
    blood_type character varying(10),
    units_requested integer NOT NULL,
    units_fulfilled integer DEFAULT 0 NOT NULL,
    urgency character varying(20) DEFAULT 'Routine'::character varying NOT NULL,
    status character varying(30) DEFAULT 'Pending'::character varying NOT NULL,
    reason text,
    created_by character varying(50) NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone
);


--
-- Name: blood_units; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.blood_units (
    id character varying(50) NOT NULL,
    intake_tag character varying(50),
    donor_first_name character varying(100),
    donor_middle_name character varying(100),
    donor_last_name character varying(100),
    donor_name character varying(200),
    village character varying(200),
    phone character varying(50),
    blood_type character varying(10),
    component character varying(50),
    purpose character varying(50),
    patient_name character varying(200),
    patient_ward character varying(100),
    requester_name character varying(200),
    requester_section character varying(100),
    status character varying(30) DEFAULT 'Screening'::character varying NOT NULL,
    bag_serial character varying(100),
    reason text,
    screened_by character varying(50),
    screened_at timestamp without time zone,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone,
    registered_by character varying(50),
    health_id character varying(10),
    inhosp_id character varying(20),
    doctor_id character varying(7),
    ward_id integer,
    bed_id integer,
    request_id character varying(50),
    screening_result character varying(20),
    cbc_result_summary text,
    approved_by character varying(50),
    approved_at timestamp without time zone,
    dispensed_by character varying(50),
    dispensed_at timestamp without time zone,
    collected_by text,
    target_patient_first_name text,
    target_patient_middle_name text,
    target_patient_last_name text,
    recipient_first_name text,
    recipient_middle_name text,
    recipient_last_name text,
    dispense_recipient_category text
);


--
-- Name: clinic_schedule; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.clinic_schedule (
    sche_week character varying(1) NOT NULL,
    sche_noon character varying(5) NOT NULL,
    sche_room character varying(3) NOT NULL,
    sche_dpt_name character varying(200),
    sche_doctor character varying(7),
    sche_doctor_name character varying(200),
    sche_open_flag character varying(1),
    sche_call_no bigint,
    modify_user character varying(7),
    modify_time timestamp without time zone,
    sche_dpt_code character varying(6),
    sche_remark character varying(1000),
    sche_call_time timestamp without time zone,
    shift character varying(10) NOT NULL
);


--
-- Name: COLUMN clinic_schedule.sche_week; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.clinic_schedule.sche_week IS '星期別';


--
-- Name: COLUMN clinic_schedule.sche_noon; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.clinic_schedule.sche_noon IS '午別';


--
-- Name: COLUMN clinic_schedule.sche_room; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.clinic_schedule.sche_room IS '診間號碼';


--
-- Name: COLUMN clinic_schedule.sche_dpt_name; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.clinic_schedule.sche_dpt_name IS '科別名稱';


--
-- Name: COLUMN clinic_schedule.sche_doctor; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.clinic_schedule.sche_doctor IS '醫師職編';


--
-- Name: COLUMN clinic_schedule.sche_doctor_name; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.clinic_schedule.sche_doctor_name IS '醫師姓名';


--
-- Name: COLUMN clinic_schedule.sche_open_flag; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.clinic_schedule.sche_open_flag IS '診次是否開放';


--
-- Name: COLUMN clinic_schedule.sche_call_no; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.clinic_schedule.sche_call_no IS '叫號號碼';


--
-- Name: COLUMN clinic_schedule.sche_dpt_code; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.clinic_schedule.sche_dpt_code IS '科別代碼';


--
-- Name: COLUMN clinic_schedule.sche_call_time; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.clinic_schedule.sche_call_time IS 'Calling Time Update';


--
-- Name: hisorderplan; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.hisorderplan (
    orderplanid bigint NOT NULL,
    inhospid character varying NOT NULL,
    health_id character(10) NOT NULL,
    hplan_type character varying(20) NOT NULL,
    seq_no smallint NOT NULL,
    plan_code character varying(20),
    plan_des character varying(150),
    free_charge character(1) DEFAULT 'N'::bpchar,
    exec_date_from timestamp without time zone,
    exec_date_to timestamp without time zone,
    order_dept character(6),
    order_dr character(7),
    plan_days smallint DEFAULT 0,
    qty_dose numeric(9,2),
    qty_daily numeric(9,2),
    unit_dose character varying(20),
    freq_code character varying(20),
    dose_indication character varying(20),
    dose_path character varying(20),
    made_type character varying(20),
    total_qty numeric(10,2),
    exam_loc character varying(20),
    urg_flag character(1) DEFAULT 'N'::bpchar,
    preop_flag character(1) DEFAULT 'N'::bpchar,
    add_flag character(1) DEFAULT 'N'::bpchar,
    keepspec_flag character(1) DEFAULT 'N'::bpchar,
    location_code character varying(10),
    trigger_tablecode character varying(30),
    trigger_recid bigint,
    dc_status character(1) DEFAULT '0'::bpchar,
    status character(1) DEFAULT '0'::bpchar NOT NULL,
    exec_status character(1),
    charge_status character(1),
    create_user character varying(7),
    dc_user character varying(7),
    modify_user character varying(7),
    print_user character varying(7),
    create_date timestamp without time zone DEFAULT now(),
    dc_date timestamp without time zone,
    modify_date timestamp without time zone,
    print_date timestamp without time zone,
    remark character varying(300),
    med_bag smallint
);


--
-- Name: hisorderplan_attr; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.hisorderplan_attr (
    orderplanatrrid bigint NOT NULL,
    orderplanid bigint NOT NULL,
    attr_code character varying(30),
    parameter_1 character varying(50),
    parameter_2 character varying(50),
    parameter_3 character varying(50),
    parameter_4 character varying(50),
    parameter_5 character varying(50),
    parameter_6 character varying(50),
    des character varying(1000)
);


--
-- Name: hisorderplan_attr_orderplanatrrid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.hisorderplan_attr_orderplanatrrid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: hisorderplan_attr_orderplanatrrid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.hisorderplan_attr_orderplanatrrid_seq OWNED BY public.hisorderplan_attr.orderplanatrrid;


--
-- Name: hisorderplan_attr_orderplanid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.hisorderplan_attr_orderplanid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: hisorderplan_attr_orderplanid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.hisorderplan_attr_orderplanid_seq OWNED BY public.hisorderplan_attr.orderplanid;


--
-- Name: hisorderplan_inhospid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.hisorderplan_inhospid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: hisorderplan_inhospid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.hisorderplan_inhospid_seq OWNED BY public.hisorderplan.inhospid;


--
-- Name: hisorderplan_orderplanid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.hisorderplan_orderplanid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: hisorderplan_orderplanid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.hisorderplan_orderplanid_seq OWNED BY public.hisorderplan.orderplanid;


--
-- Name: hisorderplan_plan_days_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.hisorderplan_plan_days_seq
    AS smallint
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: hisorderplan_plan_days_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.hisorderplan_plan_days_seq OWNED BY public.hisorderplan.plan_days;


--
-- Name: hisorderplan_seq_no_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.hisorderplan_seq_no_seq
    AS smallint
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: hisorderplan_seq_no_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.hisorderplan_seq_no_seq OWNED BY public.hisorderplan.seq_no;


--
-- Name: hisordersoa; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.hisordersoa (
    soaid bigint NOT NULL,
    inhospid character varying NOT NULL,
    health_id character(10) NOT NULL,
    kind character varying(20) NOT NULL,
    context text,
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL,
    source_type character varying(10) NOT NULL,
    version_code integer,
    status "char",
    dc_user character varying(7),
    dc_date timestamp without time zone,
    modify_user character varying,
    modify_date timestamp without time zone,
    "OriginalRemark" text,
    "Ai_modified" text,
    "Aigeneratedcontent" text
);


--
-- Name: hisorderplan_soa_inhospid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.hisorderplan_soa_inhospid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: hisorderplan_soa_inhospid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.hisorderplan_soa_inhospid_seq OWNED BY public.hisordersoa.inhospid;


--
-- Name: hisorderplan_soa_soaid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.hisorderplan_soa_soaid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: hisorderplan_trigger_recid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.hisorderplan_trigger_recid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: hisorderplan_trigger_recid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.hisorderplan_trigger_recid_seq OWNED BY public.hisorderplan.trigger_recid;


--
-- Name: home_physicalsign; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.home_physicalsign (
    phyid integer NOT NULL,
    inhospid text,
    date timestamp without time zone NOT NULL,
    before_breakfast text,
    after_breakfast text,
    before_dinner text,
    after_dinner text,
    modify_user text,
    modify_time timestamp without time zone,
    category text,
    evining_diastolic text,
    evining_systolic text,
    morning_diastolic text,
    morning_systolic text
);


--
-- Name: home_physicalsign_phyid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.home_physicalsign ALTER COLUMN phyid ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.home_physicalsign_phyid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: inptient_reservation; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.inptient_reservation (
    reservation_date timestamp without time zone NOT NULL,
    healthid character varying(20) NOT NULL,
    inhospid character varying(20) NOT NULL,
    department text,
    ward_id text,
    bed_id text,
    status text,
    "dischargeType" text,
    discharge_date timestamp without time zone DEFAULT now(),
    discharge_approver character varying(7),
    create_by character varying(7),
    create_at timestamp without time zone DEFAULT now() NOT NULL,
    modify_by character varying(7),
    modify_at timestamp without time zone DEFAULT now(),
    referral_place character varying(100),
    transfer_place character varying(100),
    doctor character varying(50),
    nurse character varying(50)
);


--
-- Name: kmu_ncd; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_ncd (
    ncdid bigint NOT NULL,
    inhospid character(20),
    healthid character(10),
    plancode character(10),
    createdate date,
    createuser character(7),
    modifyuser character(7),
    patient_answer text
);


--
-- Name: kmu_Ncd_NcdId_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.kmu_ncd ALTER COLUMN ncdid ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."kmu_Ncd_NcdId_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    MAXVALUE 2147483647
    CACHE 1
);


--
-- Name: kmu_attribute; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_attribute (
    attr_code character varying(3) NOT NULL,
    attr_name character varying(100),
    attr_reg_fee bigint,
    attr_status character varying(1),
    modify_user character varying(7),
    modify_time timestamp without time zone,
    attr_penalty_fee bigint
);


--
-- Name: COLUMN kmu_attribute.attr_code; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_attribute.attr_code IS '身分代碼';


--
-- Name: COLUMN kmu_attribute.attr_name; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_attribute.attr_name IS '身分說明';


--
-- Name: COLUMN kmu_attribute.attr_reg_fee; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_attribute.attr_reg_fee IS '該身分預收的掛號費用';


--
-- Name: COLUMN kmu_attribute.attr_status; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_attribute.attr_status IS '啟用狀態';


--
-- Name: kmu_auths; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_auths (
    user_idno character varying(7) NOT NULL,
    project_id character varying(32) NOT NULL,
    creator character varying(7) NOT NULL,
    create_time timestamp without time zone NOT NULL
);


--
-- Name: TABLE kmu_auths; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.kmu_auths IS 'User Auth File(Account permissions)';


--
-- Name: kmu_auths_log; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_auths_log (
    user_idno character varying(7) NOT NULL,
    edit_type character varying(10) NOT NULL,
    project_id character varying(32) NOT NULL,
    edit_time timestamp without time zone DEFAULT now() NOT NULL,
    edit_user character varying(7) NOT NULL
);


--
-- Name: TABLE kmu_auths_log; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.kmu_auths_log IS 'Auth change log';


--
-- Name: kmu_chart; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_chart (
    chr_health_id character(10) NOT NULL,
    chr_national_id character varying(10),
    chr_patient_firstname character varying(200) NOT NULL,
    chr_patient_midname character varying(200),
    chr_patient_lastname character varying(200),
    chr_sex character varying(1) NOT NULL,
    chr_birth_date date,
    chr_mobile_phone character varying(30) NOT NULL,
    chr_address text,
    chr_emg_contact character varying(650),
    chr_contact_relation character(2),
    chr_contact_phone character varying(30),
    chr_combine_flag character(1),
    chr_remark character varying(1000),
    modify_user character(7),
    modify_time timestamp without time zone NOT NULL,
    chr_area_code character varying(20),
    chr_refugee_flag character(1) DEFAULT 'N'::bpchar,
    upload_status character varying(1) DEFAULT 'N'::character varying,
    upload_time timestamp without time zone,
    chr_drug_allergy text,
    chr_other_medical_history text,
    chr_ncd_history text
);


--
-- Name: COLUMN kmu_chart.chr_refugee_flag; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_chart.chr_refugee_flag IS 'refugee: Y';


--
-- Name: kmu_chart_MergeHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."kmu_chart_MergeHistory" (
    "Id" integer NOT NULL,
    chr_halth_id text,
    mh_health_id text,
    merged_time timestamp without time zone NOT NULL,
    merger_user text,
    "ChrNationalId" text,
    "ChrPatientFirstname" text,
    "ChrPatientMidname" text,
    "ChrPatientLastname" text,
    "ChrSex" text,
    "ChrBirthDate" date,
    "ChrMobilePhone" text,
    "ChrAddress" text,
    "ChrEmgContact" text,
    "ChrContactRelation" text,
    "ChrContactPhone" text,
    "ChrCombineFlag" character(1),
    "ChrRemark" text,
    "ModifyUser" text,
    "ModifyTime" timestamp without time zone NOT NULL,
    "ChrAreaCode" text,
    "ChrRefugeeFlag" character(1),
    upload_status character varying(1) DEFAULT 'N'::character varying,
    upload_time timestamp without time zone
);


--
-- Name: kmu_chart_MergeHistory_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."kmu_chart_MergeHistory" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."kmu_chart_MergeHistory_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: kmu_chart_log; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_chart_log (
    log_id character varying(20) NOT NULL,
    log_user character(7),
    log_time timestamp without time zone,
    log_mode character(1),
    chr_health_id character(10),
    chr_national_id character varying(10),
    chr_patient_firstname character varying(200),
    chr_patient_midname character varying(200),
    chr_patient_lastname character varying(200),
    chr_sex character varying(1),
    chr_birth_date date,
    chr_mobile_phone character varying(30),
    chr_address text,
    chr_emg_contact character varying(650),
    chr_contact_relation character(2),
    chr_contact_phone character varying(30),
    chr_combine_flag character(1),
    chr_remark character varying(1000),
    modify_user character(7),
    modify_time timestamp without time zone,
    chr_area_code character varying(20),
    chr_refugee_flag character(1)
);


--
-- Name: COLUMN kmu_chart_log.log_user; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_chart_log.log_user IS 'Modify by User';


--
-- Name: COLUMN kmu_chart_log.log_time; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_chart_log.log_time IS 'Modify Time';


--
-- Name: COLUMN kmu_chart_log.log_mode; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_chart_log.log_mode IS 'Command Mode: Insert, Update, Delete';


--
-- Name: kmu_coderef; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_coderef (
    ref_codetype character varying(100),
    ref_code character varying(500),
    ref_name character varying(1000),
    ref_des character varying(2000),
    ref_id character varying(20) NOT NULL,
    ref_casetype character varying(5),
    ref_showseq integer,
    ref_des2 character varying(2000),
    modify_id character varying(7),
    modify_time timestamp without time zone,
    ref_default_flag character varying(5)
);


--
-- Name: COLUMN kmu_coderef.ref_default_flag; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_coderef.ref_default_flag IS '是否預設啟用';


--
-- Name: kmu_condition; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_condition (
    cnd_codetype character varying(100) NOT NULL,
    cnd_code character varying(500) NOT NULL,
    cnd_value1 character varying(100),
    cnd_symbol1 character(2),
    cnd_value2 character varying(100),
    cnd_symbol2 character(2),
    cnd_enable character(1),
    cnd_week character(1),
    cnd_noon character varying(5),
    cnd_room character varying(3),
    cnd_desc character varying(500),
    modify_user character varying(7),
    modify_time timestamp without time zone
);


--
-- Name: kmu_department; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_department (
    dpt_code character varying(6) NOT NULL,
    dpt_name character varying(200),
    dpt_category character varying(3),
    dpt_depth bigint,
    dpt_status character varying(1),
    dpt_remark character varying(1000),
    dpt_default_attr character varying(3),
    modify_user character varying(7),
    modify_time timestamp without time zone,
    dpt_parent character varying(6)
);


--
-- Name: COLUMN kmu_department.dpt_default_attr; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_department.dpt_default_attr IS '預設身分別';


--
-- Name: kmu_icd; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_icd (
    icd_code character varying(8) NOT NULL,
    icd_english_name character varying(400) NOT NULL,
    icd_code_undot character varying(7) NOT NULL,
    parent_code character varying(8),
    status character varying(1),
    icd_type character varying(10),
    show_mode character varying(10),
    versioncode character varying(2),
    modify_user character varying(7) NOT NULL,
    modify_date timestamp without time zone NOT NULL,
    dhis2_code integer
);


--
-- Name: TABLE kmu_icd; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.kmu_icd IS 'Diagnosis data.';


--
-- Name: COLUMN kmu_icd.icd_code_undot; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_icd.icd_code_undot IS 'ICD Code without decimal point.';


--
-- Name: COLUMN kmu_icd.parent_code; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_icd.parent_code IS 'Parent ICD Code for HisOrder UI Design.';


--
-- Name: COLUMN kmu_icd.status; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_icd.status IS 'ICD Code.';


--
-- Name: COLUMN kmu_icd.icd_type; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_icd.icd_type IS 'CM/PCS';


--
-- Name: COLUMN kmu_icd.show_mode; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_icd.show_mode IS 'Show position for HisOrder UI Design.';


--
-- Name: COLUMN kmu_icd.versioncode; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_icd.versioncode IS 'ICD 9 / ICD 10 ...';


--
-- Name: kmu_medfrequency; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_medfrequency (
    frq_code character varying(20) NOT NULL,
    freq_desc character varying(100) NOT NULL,
    frq_for_days integer,
    frq_for_times integer,
    frq_one_day_times integer,
    enable_status character(1) NOT NULL,
    create_user character varying(7),
    modify_user character varying(7),
    frq_seq_no integer DEFAULT 999
);


--
-- Name: kmu_medfrequency_ind; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_medfrequency_ind (
    frq_code character varying(20) NOT NULL,
    ind_code character varying(20) NOT NULL,
    ind_desc character varying(100),
    showseq numeric(3,0),
    enable_status character(1) NOT NULL,
    create_user character varying(7),
    modify_user character varying(7)
);


--
-- Name: kmu_medicine; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_medicine (
    med_id character varying(10) NOT NULL,
    med_type character varying(2) NOT NULL,
    generic_name character varying(150),
    brand_name character varying(150),
    unit_spec character varying(20),
    pack_spec character varying(20),
    default_freq character varying(10),
    ref_duration character varying(3),
    remarks character varying(500),
    start_date timestamp without time zone DEFAULT now(),
    end_date timestamp without time zone,
    status character(1) DEFAULT '0'::bpchar,
    create_date timestamp without time zone DEFAULT now(),
    create_user character varying(7),
    modify_date timestamp without time zone DEFAULT now(),
    modify_user character varying(7)
);


--
-- Name: COLUMN kmu_medicine.med_type; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_medicine.med_type IS '1-口服
2-針劑
3-外用';


--
-- Name: COLUMN kmu_medicine.unit_spec; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_medicine.unit_spec IS '醫囑單位';


--
-- Name: COLUMN kmu_medicine.pack_spec; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_medicine.pack_spec IS '包裝單位(藥局發藥)';


--
-- Name: COLUMN kmu_medicine.default_freq; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_medicine.default_freq IS '開立時預設頻次(可空白)';


--
-- Name: COLUMN kmu_medicine.ref_duration; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_medicine.ref_duration IS '建議的用藥天數(不用預設)';


--
-- Name: COLUMN kmu_medicine.remarks; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_medicine.remarks IS '其他備註說明';


--
-- Name: COLUMN kmu_medicine.status; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_medicine.status IS '醫囑系統是否顯示';


--
-- Name: kmu_medpathway; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_medpathway (
    med_type character(1) NOT NULL,
    path_code character varying(20) NOT NULL,
    path_desc character varying(100) NOT NULL,
    showseq numeric(3,0),
    enable_status character(1) NOT NULL,
    create_user character varying(7),
    modify_user character varying(7)
);


--
-- Name: kmu_mental; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_mental (
    mntid integer NOT NULL,
    inhospid text,
    healthid text,
    plancode text,
    plandes text,
    createdate timestamp without time zone,
    createuser text,
    modifyuser text,
    patient_answer text
);


--
-- Name: kmu_mental_mntid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.kmu_mental ALTER COLUMN mntid ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.kmu_mental_mntid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: kmu_non_medicine; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_non_medicine (
    item_id character varying(10) NOT NULL,
    item_name character varying(150) NOT NULL,
    item_type character varying(10) NOT NULL,
    item_spec character varying(20),
    start_date timestamp without time zone,
    end_date timestamp without time zone,
    status character(1) DEFAULT '0'::bpchar NOT NULL,
    create_user character varying(7),
    create_date timestamp without time zone DEFAULT now(),
    modify_user character varying(7),
    modify_date timestamp without time zone DEFAULT now(),
    remark character varying(500),
    show_seq numeric(7,2),
    group_code character varying(10),
    enabled boolean DEFAULT false NOT NULL
);


--
-- Name: COLUMN kmu_non_medicine.item_type; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_non_medicine.item_type IS '5.Laboratory 6.Radiology 7.Pathology 8.Material';


--
-- Name: kmu_projects; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_projects (
    project_id character varying(32) NOT NULL,
    project_name character varying(64) NOT NULL,
    url character varying(200) NOT NULL,
    creator character varying(7) NOT NULL,
    create_time timestamp without time zone DEFAULT now() NOT NULL
);


--
-- Name: TABLE kmu_projects; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.kmu_projects IS 'Auth Setting reference Project File(main function node)';


--
-- Name: kmu_serialpool; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_serialpool (
    serial_owner character varying(50) NOT NULL,
    serial_no bigint,
    serial_prefix character varying(5),
    serial_maxno character varying(20)
);


--
-- Name: kmu_upload_log; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_upload_log (
    logid bigint NOT NULL,
    reg_date date,
    inhospid character varying,
    exec_datetime timestamp without time zone,
    option "char",
    result_success boolean,
    result_message text,
    result_status_code character varying,
    result_status_desc character varying,
    target_url character varying,
    target_agency character varying,
    local_ip character varying,
    local_login_user character varying,
    exec_batch_seq_no smallint
);


--
-- Name: kmu_upload_log_logid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.kmu_upload_log_logid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: kmu_users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_users (
    user_idno character varying(7) NOT NULL,
    user_password text NOT NULL,
    user_name_midname text NOT NULL,
    user_birth_date timestamp without time zone,
    user_sex text,
    start_date timestamp without time zone,
    end_date timestamp without time zone,
    user_mobile_phone character varying(30) NOT NULL,
    user_email text,
    creator character varying(7) NOT NULL,
    create_time timestamp without time zone NOT NULL,
    user_name_firstname text NOT NULL,
    user_name_lastname text NOT NULL,
    user_category character varying(1) NOT NULL,
    account_status character varying(1) DEFAULT 1 NOT NULL
);


--
-- Name: TABLE kmu_users; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.kmu_users IS 'Account Basic File(user account )';


--
-- Name: COLUMN kmu_users.user_category; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.kmu_users.user_category IS '分類(1:Doctor,2:Nurse,3.Staff';


--
-- Name: kmu_users_log; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kmu_users_log (
    user_idno text NOT NULL,
    event_type text NOT NULL,
    is_success boolean NOT NULL,
    event_error_input text,
    message text,
    event_time timestamp without time zone DEFAULT now() NOT NULL,
    ip character varying(64) NOT NULL,
    event_logging_user character varying(7)
);


--
-- Name: TABLE kmu_users_log; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.kmu_users_log IS 'Account change log(帳號基本檔修改紀錄表2023.03.03)';


--
-- Name: medical_administration; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.medical_administration (
    id integer NOT NULL,
    health_id text,
    inhosp_id text,
    medical_type text,
    shift text,
    med_code text,
    med_des text,
    milk_amount text,
    administered_by character varying(7),
    modify_at timestamp without time zone DEFAULT now() NOT NULL
);


--
-- Name: medical_administration_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.medical_administration ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.medical_administration_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: pathology_accession_sequence; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_accession_sequence (
    section_code character varying(10) NOT NULL,
    sequence_date date NOT NULL,
    last_number integer DEFAULT 0 NOT NULL
);


--
-- Name: pathology_audit_log; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_audit_log (
    audit_id bigint NOT NULL,
    entity_type character varying(50) NOT NULL,
    entity_id bigint NOT NULL,
    action character varying(50) NOT NULL,
    user_id character varying(7) NOT NULL,
    user_name character varying(100),
    details text,
    ip_address character varying(45),
    created_at timestamp without time zone NOT NULL
);


--
-- Name: pathology_audit_log_audit_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_audit_log_audit_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_audit_log_audit_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_audit_log_audit_id_seq OWNED BY public.pathology_audit_log.audit_id;


--
-- Name: pathology_invoice; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_invoice (
    invoice_id bigint NOT NULL,
    order_id bigint NOT NULL,
    invoice_no character varying(30) NOT NULL,
    subtotal numeric(12,2) DEFAULT 0 NOT NULL,
    discount numeric(12,2) DEFAULT 0 NOT NULL,
    tax numeric(12,2) DEFAULT 0 NOT NULL,
    total numeric(12,2) DEFAULT 0 NOT NULL,
    status character varying(20) NOT NULL,
    insurance_provider character varying(100),
    insurance_policy_no character varying(50),
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL
);


--
-- Name: pathology_invoice_invoice_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_invoice_invoice_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_invoice_invoice_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_invoice_invoice_id_seq OWNED BY public.pathology_invoice.invoice_id;


--
-- Name: pathology_order; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_order (
    order_id bigint NOT NULL,
    orderplanid bigint,
    accession_no character varying(30) NOT NULL,
    patient_id character varying(20) NOT NULL,
    inhospid character varying(30) NOT NULL,
    patient_name character varying(200) NOT NULL,
    ward character varying(50),
    bed_location character varying(50),
    referring_doctor_id bigint,
    referring_doctor_name character varying(100),
    status character varying(30) NOT NULL,
    priority character varying(20) DEFAULT 'Routine'::character varying NOT NULL,
    clinical_notes character varying(1000),
    order_date_time timestamp without time zone NOT NULL,
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL,
    modify_user character varying(7),
    modify_date timestamp without time zone,
    section_code character varying(10) DEFAULT 'HIST'::character varying NOT NULL,
    report_template_code character varying(30),
    is_walk_in boolean DEFAULT false NOT NULL,
    patient_age character varying(10),
    patient_gender character varying(10),
    patient_phone character varying(20)
);


--
-- Name: pathology_order_item; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_order_item (
    order_item_id bigint NOT NULL,
    order_id bigint NOT NULL,
    test_id bigint,
    test_code character varying(30) NOT NULL,
    test_name character varying(200) NOT NULL,
    price numeric(12,2) DEFAULT 0 NOT NULL,
    status character varying(30) NOT NULL
);


--
-- Name: pathology_order_item_order_item_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_order_item_order_item_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_order_item_order_item_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_order_item_order_item_id_seq OWNED BY public.pathology_order_item.order_item_id;


--
-- Name: pathology_order_order_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_order_order_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_order_order_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_order_order_id_seq OWNED BY public.pathology_order.order_id;


--
-- Name: pathology_payment; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_payment (
    payment_id bigint NOT NULL,
    invoice_id bigint NOT NULL,
    amount numeric(12,2) NOT NULL,
    method character varying(30) NOT NULL,
    reference_no character varying(50),
    paid_at timestamp without time zone NOT NULL,
    collected_by character varying(7) NOT NULL
);


--
-- Name: pathology_payment_payment_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_payment_payment_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_payment_payment_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_payment_payment_id_seq OWNED BY public.pathology_payment.payment_id;


--
-- Name: pathology_purchase_order; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_purchase_order (
    purchase_order_id bigint NOT NULL,
    po_no character varying(30) NOT NULL,
    supplier character varying(100) NOT NULL,
    status character varying(20) NOT NULL,
    total_amount numeric(12,2) DEFAULT 0 NOT NULL,
    ordered_at timestamp without time zone NOT NULL,
    notes character varying(500),
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL
);


--
-- Name: pathology_purchase_order_purchase_order_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_purchase_order_purchase_order_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_purchase_order_purchase_order_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_purchase_order_purchase_order_id_seq OWNED BY public.pathology_purchase_order.purchase_order_id;


--
-- Name: pathology_reagent; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_reagent (
    reagent_id bigint NOT NULL,
    sku character varying(30) NOT NULL,
    name character varying(200) NOT NULL,
    unit character varying(20) DEFAULT 'Unit'::character varying NOT NULL,
    min_stock numeric(12,2) DEFAULT 0 NOT NULL,
    supplier character varying(100),
    is_active boolean DEFAULT true NOT NULL,
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL
);


--
-- Name: pathology_reagent_lot; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_reagent_lot (
    lot_id bigint NOT NULL,
    reagent_id bigint NOT NULL,
    lot_no character varying(50) NOT NULL,
    quantity numeric(12,2) NOT NULL,
    expiry_date timestamp without time zone,
    supplier character varying(100),
    received_at timestamp without time zone NOT NULL,
    create_user character varying(7) NOT NULL
);


--
-- Name: pathology_reagent_lot_lot_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_reagent_lot_lot_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_reagent_lot_lot_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_reagent_lot_lot_id_seq OWNED BY public.pathology_reagent_lot.lot_id;


--
-- Name: pathology_reagent_reagent_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_reagent_reagent_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_reagent_reagent_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_reagent_reagent_id_seq OWNED BY public.pathology_reagent.reagent_id;


--
-- Name: pathology_referring_doctor; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_referring_doctor (
    doctor_id bigint NOT NULL,
    code character varying(20) NOT NULL,
    name character varying(100) NOT NULL,
    specialty character varying(100),
    phone character varying(30),
    email character varying(100),
    commission_rate numeric(5,2) DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL
);


--
-- Name: pathology_referring_doctor_doctor_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_referring_doctor_doctor_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_referring_doctor_doctor_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_referring_doctor_doctor_id_seq OWNED BY public.pathology_referring_doctor.doctor_id;


--
-- Name: pathology_report; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_report (
    report_id bigint NOT NULL,
    order_id bigint NOT NULL,
    report_no character varying(30) NOT NULL,
    qr_verification_code character varying(64) NOT NULL,
    pdf_path character varying(500),
    status character varying(20) NOT NULL,
    released_at timestamp without time zone,
    released_by character varying(7),
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL
);


--
-- Name: pathology_report_content; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_report_content (
    content_id bigint NOT NULL,
    order_id bigint NOT NULL,
    report_template_code character varying(30) NOT NULL,
    clinical_diagnosis character varying(500),
    nature_of_specimen character varying(200),
    clinical_history character varying(2000),
    gross text,
    microscopy text,
    diagnosis character varying(1000),
    comment character varying(2000),
    specimen_adequacy character varying(20),
    specimen_collection_date timestamp without time zone,
    quality_of_smear character varying(500),
    background character varying(500),
    squamous_cells character varying(500),
    glandular_metaplastic_cells character varying(500),
    others character varying(1000),
    gross_image_path character varying(500),
    microscopic_image_path character varying(500),
    pathologist_name character varying(100),
    report_date timestamp without time zone,
    status character varying(20) DEFAULT 'Draft'::character varying NOT NULL,
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone DEFAULT now() NOT NULL,
    modify_user character varying(7),
    modify_date timestamp without time zone
);


--
-- Name: pathology_report_content_content_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_report_content_content_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_report_content_content_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_report_content_content_id_seq OWNED BY public.pathology_report_content.content_id;


--
-- Name: pathology_report_report_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_report_report_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_report_report_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_report_report_id_seq OWNED BY public.pathology_report.report_id;


--
-- Name: pathology_report_template; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_report_template (
    template_code character varying(30) NOT NULL,
    title character varying(100) NOT NULL,
    subtitle character varying(200),
    accession_suffix character varying(10) NOT NULL,
    section_code character varying(10) NOT NULL,
    icon_class character varying(50) DEFAULT 'fa-file-medical'::character varying NOT NULL,
    accent_color character varying(20) DEFAULT '#6d28d9'::character varying NOT NULL,
    sort_order integer DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT true NOT NULL
);


--
-- Name: pathology_result; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_result (
    result_id bigint NOT NULL,
    order_item_id bigint NOT NULL,
    sample_id bigint,
    value character varying(500),
    unit character varying(30),
    reference_range character varying(100),
    flag character varying(20),
    notes character varying(1000),
    status character varying(30) NOT NULL,
    entered_by character varying(7),
    entered_by_name character varying(100),
    entered_at timestamp without time zone,
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL,
    modify_user character varying(7),
    modify_date timestamp without time zone
);


--
-- Name: pathology_result_approval; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_result_approval (
    approval_id bigint NOT NULL,
    result_id bigint NOT NULL,
    level character varying(30) NOT NULL,
    approver_id character varying(7) NOT NULL,
    approver_name character varying(100) NOT NULL,
    status character varying(20) NOT NULL,
    signed_at timestamp without time zone,
    signature_hash character varying(128),
    comments character varying(500)
);


--
-- Name: pathology_result_approval_approval_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_result_approval_approval_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_result_approval_approval_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_result_approval_approval_id_seq OWNED BY public.pathology_result_approval.approval_id;


--
-- Name: pathology_result_result_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_result_result_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_result_result_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_result_result_id_seq OWNED BY public.pathology_result.result_id;


--
-- Name: pathology_sample; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_sample (
    sample_id bigint NOT NULL,
    order_id bigint NOT NULL,
    barcode character varying(40) NOT NULL,
    sample_type character varying(50) DEFAULT 'Blood'::character varying NOT NULL,
    status character varying(30) NOT NULL,
    collected_at timestamp without time zone,
    collected_by character varying(7),
    collected_by_name character varying(100),
    rejection_reason character varying(500),
    recollection_of_sample_id bigint,
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL
);


--
-- Name: pathology_sample_sample_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_sample_sample_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_sample_sample_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_sample_sample_id_seq OWNED BY public.pathology_sample.sample_id;


--
-- Name: pathology_test; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_test (
    test_id bigint NOT NULL,
    category_id integer NOT NULL,
    code character varying(30) NOT NULL,
    name character varying(200) NOT NULL,
    price numeric(12,2) DEFAULT 0 NOT NULL,
    sample_type character varying(50) DEFAULT 'Blood'::character varying NOT NULL,
    turnaround_hours integer DEFAULT 24 NOT NULL,
    formula character varying(500),
    unit character varying(30),
    reference_range character varying(100),
    sample_requirements character varying(500),
    is_active boolean DEFAULT true NOT NULL,
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL,
    modify_user character varying(7),
    modify_date timestamp without time zone
);


--
-- Name: pathology_test_category; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_test_category (
    category_id integer NOT NULL,
    code character varying(20) NOT NULL,
    name character varying(100) NOT NULL,
    sort_order integer NOT NULL,
    is_active boolean DEFAULT true NOT NULL
);


--
-- Name: pathology_test_category_category_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_test_category_category_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_test_category_category_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_test_category_category_id_seq OWNED BY public.pathology_test_category.category_id;


--
-- Name: pathology_test_package; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_test_package (
    package_id bigint NOT NULL,
    code character varying(30) NOT NULL,
    name character varying(200) NOT NULL,
    price numeric(12,2) DEFAULT 0 NOT NULL,
    description character varying(500),
    is_active boolean DEFAULT true NOT NULL,
    create_user character varying(7) NOT NULL,
    create_date timestamp without time zone NOT NULL
);


--
-- Name: pathology_test_package_item; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pathology_test_package_item (
    package_item_id bigint NOT NULL,
    package_id bigint NOT NULL,
    test_id bigint NOT NULL
);


--
-- Name: pathology_test_package_item_package_item_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_test_package_item_package_item_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_test_package_item_package_item_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_test_package_item_package_item_id_seq OWNED BY public.pathology_test_package_item.package_item_id;


--
-- Name: pathology_test_package_package_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_test_package_package_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_test_package_package_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_test_package_package_id_seq OWNED BY public.pathology_test_package.package_id;


--
-- Name: pathology_test_test_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pathology_test_test_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: pathology_test_test_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pathology_test_test_id_seq OWNED BY public.pathology_test.test_id;


--
-- Name: physical_sign; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.physical_sign (
    phy_id character varying(50) NOT NULL,
    inhospid character varying(20) NOT NULL,
    phy_type character varying(30) NOT NULL,
    phy_value character varying(500),
    modify_user character varying(7),
    modify_time timestamp without time zone,
    phy_version smallint
);


--
-- Name: radiology_report_audit_trails; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.radiology_report_audit_trails (
    audit_id bigint NOT NULL,
    report_id integer NOT NULL,
    event_type text NOT NULL,
    payload jsonb NOT NULL,
    actor_id text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: radiology_report_audit_trails_audit_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.radiology_report_audit_trails ALTER COLUMN audit_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.radiology_report_audit_trails_audit_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: radiologyexamrequests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.radiologyexamrequests (
    id integer NOT NULL,
    ordereddate date,
    orderid text,
    orderplanid bigint,
    inhospid text,
    patientid text,
    roomid integer,
    item_id text,
    seq_no integer,
    accessionnumber text,
    studyinstanceuid text,
    orthancstudyid text,
    technicianid text,
    sourcetype text,
    requestedby text,
    remark text,
    status text,
    createdat timestamp without time zone DEFAULT now() NOT NULL,
    CONSTRAINT ck_radiologyexamrequests_status CHECK (((status IS NULL) OR (status = ANY (ARRAY['Ordered'::text, 'Scheduled'::text, 'In_Progress'::text, 'Interpreted'::text, 'Finalized'::text, 'Cancelled'::text, 'PENDING'::text, 'SCHEDULED'::text, 'COMPLETED'::text, 'FINALIZED'::text, 'CANCELLED'::text]))))
);


--
-- Name: radiologyexamrequests_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.radiologyexamrequests ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.radiologyexamrequests_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: radiologyreport_versions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.radiologyreport_versions (
    version_id bigint NOT NULL,
    report_id integer NOT NULL,
    version_no integer NOT NULL,
    snapshot jsonb NOT NULL,
    created_by text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: radiologyreport_versions_version_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.radiologyreport_versions ALTER COLUMN version_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.radiologyreport_versions_version_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: radiologyreports; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.radiologyreports (
    reportid integer NOT NULL,
    radrequestid integer NOT NULL,
    inhospid text NOT NULL,
    radiologistid text NOT NULL,
    report_text text NOT NULL,
    status text NOT NULL,
    createdat timestamp without time zone DEFAULT now() NOT NULL,
    signedat timestamp without time zone,
    radiologyexamrequestid integer,
    studyinstanceuid text,
    impression text,
    structured_findings jsonb DEFAULT '{}'::jsonb NOT NULL,
    is_locked boolean DEFAULT false NOT NULL,
    finalized_at timestamp with time zone,
    updatedat timestamp without time zone DEFAULT now() NOT NULL
);


--
-- Name: radiologyreports_reportid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.radiologyreports ALTER COLUMN reportid ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.radiologyreports_reportid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: registration; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.registration (
    reg_date date NOT NULL,
    reg_department character varying(6) NOT NULL,
    reg_noon character varying(5) NOT NULL,
    reg_seq_no smallint NOT NULL,
    reg_health_id character(10),
    inhospid character varying(20),
    reg_triage character varying(1),
    reg_bed_no character varying(3),
    reg_attribute character varying(3),
    reg_doctor_id character varying(7),
    reg_room_no character varying(3),
    reg_status character varying(1),
    reg_call_time timestamp without time zone,
    reg_start_time timestamp without time zone,
    reg_end_time timestamp without time zone,
    modify_user character varying(7),
    modify_time timestamp without time zone,
    reg_follow_code character varying(20),
    reg_follow_desc character varying(500),
    reg_attr_desc character varying(200),
    reg_exam_start_time timestamp without time zone,
    reg_exam_end_time timestamp without time zone,
    reg_create_time timestamp without time zone,
    score character(2),
    physign_time timestamp without time zone,
    reg_observe_start_time timestamp without time zone,
    reg_observe_end_time timestamp without time zone,
    shift character varying(10),
    upload_status character varying(1) DEFAULT 'N'::character varying,
    upload_time timestamp without time zone,
    wardid character varying(50),
    bedid character varying(50),
    dischargetype character varying(50),
    dischargedate timestamp without time zone,
    dischargeapprover character varying(50),
    referral_place character varying(50),
    transfer_place character varying(50),
    doctor character varying(50),
    nurse character varying(50)
);


--
-- Name: COLUMN registration.reg_date; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_date IS '看診日';


--
-- Name: COLUMN registration.reg_department; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_department IS '看診科別';


--
-- Name: COLUMN registration.reg_noon; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_noon IS '午別';


--
-- Name: COLUMN registration.reg_seq_no; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_seq_no IS '門診:看診號
急診:檢傷序號';


--
-- Name: COLUMN registration.reg_health_id; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_health_id IS '病歷號';


--
-- Name: COLUMN registration.inhospid; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.inhospid IS '就醫序號';


--
-- Name: COLUMN registration.reg_triage; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_triage IS '檢傷分類
0：一般門診(白燈)
1：急診分類(綠燈)
2：急診分類(黃燈)
3：急診分類(紅燈)
4：急診分類(黑燈)';


--
-- Name: COLUMN registration.reg_bed_no; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_bed_no IS '急診床號';


--
-- Name: COLUMN registration.reg_attribute; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_attribute IS '特殊身分->參考kmu_attribute';


--
-- Name: COLUMN registration.reg_doctor_id; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_doctor_id IS '醫師職邊';


--
-- Name: COLUMN registration.reg_room_no; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_room_no IS '診間號';


--
-- Name: COLUMN registration.reg_status; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_status IS '看診狀態
N:未看診
T :暫存
* :已看診
C:取消掛號';


--
-- Name: COLUMN registration.reg_call_time; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_call_time IS '叫號時間';


--
-- Name: COLUMN registration.reg_start_time; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_start_time IS '開始看診時間';


--
-- Name: COLUMN registration.reg_end_time; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_end_time IS '結束看診時間';


--
-- Name: COLUMN registration.reg_attr_desc; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_attr_desc IS '身分備註';


--
-- Name: COLUMN registration.reg_exam_start_time; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_exam_start_time IS 'click examining start time';


--
-- Name: COLUMN registration.reg_exam_end_time; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_exam_end_time IS 'finish examining return time';


--
-- Name: COLUMN registration.reg_create_time; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.registration.reg_create_time IS 'create datetime for registration ';


--
-- Name: rooms; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.rooms (
    roomid integer NOT NULL,
    department text,
    modality text NOT NULL,
    room_number text NOT NULL,
    description text,
    "IsActive" boolean DEFAULT true NOT NULL
);


--
-- Name: rooms_roomid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.rooms ALTER COLUMN roomid ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.rooms_roomid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: testresults; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.testresults (
    testresultid integer NOT NULL,
    "Billno" text,
    "Billdate" text,
    "TestGroup" text,
    "Reportedby" text,
    "Result" text,
    "TestName" text,
    "Contents" text,
    "NvalueMale" text,
    "NvalueFemale" text,
    "Sufix" text,
    "OriOrder" text,
    "AttachFile" text,
    refbillorder text,
    code text,
    status character varying(255)
);


--
-- Name: testresults_testresultid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.testresults ALTER COLUMN testresultid ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.testresults_testresultid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: transaction_call; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.transaction_call (
    call_id integer NOT NULL,
    call_reg_date date,
    call_reg_department character varying(6),
    call_reg_noon character varying(5),
    call_reg_seq_no smallint,
    call_patient_id character(10),
    inhospid bigint,
    call_time timestamp without time zone,
    modify_suer character varying(7),
    modify_time timestamp without time zone DEFAULT now()
);


--
-- Name: COLUMN transaction_call.call_id; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.transaction_call.call_id IS '流水號';


--
-- Name: COLUMN transaction_call.call_reg_date; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.transaction_call.call_reg_date IS '看診日';


--
-- Name: COLUMN transaction_call.call_reg_department; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.transaction_call.call_reg_department IS '看診科';


--
-- Name: COLUMN transaction_call.call_reg_noon; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.transaction_call.call_reg_noon IS '午別';


--
-- Name: COLUMN transaction_call.call_reg_seq_no; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.transaction_call.call_reg_seq_no IS '看診號';


--
-- Name: COLUMN transaction_call.call_patient_id; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.transaction_call.call_patient_id IS '病歷號';


--
-- Name: COLUMN transaction_call.inhospid; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.transaction_call.inhospid IS '就醫序號';


--
-- Name: COLUMN transaction_call.call_time; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.transaction_call.call_time IS '叫號時間';


--
-- Name: transaction_call_call_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.transaction_call ALTER COLUMN call_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.transaction_call_call_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: transaction_fee; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.transaction_fee (
    transation_id integer NOT NULL,
    transaction_time timestamp without time zone NOT NULL,
    inhospid bigint,
    fee_type character varying(10),
    fee_paid_flag character varying(1),
    fee_paid_money integer,
    modify_user character varying(7)
);


--
-- Name: COLUMN transaction_fee.transation_id; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.transaction_fee.transation_id IS '流水號';


--
-- Name: COLUMN transaction_fee.transaction_time; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.transaction_fee.transaction_time IS '交易時間';


--
-- Name: COLUMN transaction_fee.inhospid; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.transaction_fee.inhospid IS '就醫序號';


--
-- Name: COLUMN transaction_fee.fee_type; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.transaction_fee.fee_type IS '收費項目';


--
-- Name: COLUMN transaction_fee.fee_paid_flag; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.transaction_fee.fee_paid_flag IS '是否已收費';


--
-- Name: COLUMN transaction_fee.fee_paid_money; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.transaction_fee.fee_paid_money IS '收費金額';


--
-- Name: transaction_fee_transation_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.transaction_fee ALTER COLUMN transation_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.transaction_fee_transation_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: wards; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.wards (
    wardid character varying(50) NOT NULL,
    ward_name text,
    deparment text,
    capacity integer NOT NULL,
    create_by character varying(7),
    create_at timestamp without time zone DEFAULT now() NOT NULL,
    modify_by character varying(7),
    modify_at timestamp without time zone DEFAULT now()
);


--
-- Name: blood_bank_blood_unit unit_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_blood_unit ALTER COLUMN unit_id SET DEFAULT nextval('public.blood_bank_blood_unit_unit_id_seq'::regclass);


--
-- Name: blood_bank_dispense dispense_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_dispense ALTER COLUMN dispense_id SET DEFAULT nextval('public.blood_bank_dispense_dispense_id_seq'::regclass);


--
-- Name: blood_bank_donor donor_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_donor ALTER COLUMN donor_id SET DEFAULT nextval('public.blood_bank_donor_donor_id_seq'::regclass);


--
-- Name: blood_bank_patient_event event_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_patient_event ALTER COLUMN event_id SET DEFAULT nextval('public.blood_bank_patient_event_event_id_seq'::regclass);


--
-- Name: blood_bank_request request_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_request ALTER COLUMN request_id SET DEFAULT nextval('public.blood_bank_request_request_id_seq'::regclass);


--
-- Name: blood_bank_screening screening_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_screening ALTER COLUMN screening_id SET DEFAULT nextval('public.blood_bank_screening_screening_id_seq'::regclass);


--
-- Name: hisorderplan_attr orderplanatrrid; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.hisorderplan_attr ALTER COLUMN orderplanatrrid SET DEFAULT nextval('public.hisorderplan_attr_orderplanatrrid_seq'::regclass);


--
-- Name: hisorderplan_attr orderplanid; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.hisorderplan_attr ALTER COLUMN orderplanid SET DEFAULT nextval('public.hisorderplan_attr_orderplanid_seq'::regclass);


--
-- Name: pathology_audit_log audit_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_audit_log ALTER COLUMN audit_id SET DEFAULT nextval('public.pathology_audit_log_audit_id_seq'::regclass);


--
-- Name: pathology_invoice invoice_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_invoice ALTER COLUMN invoice_id SET DEFAULT nextval('public.pathology_invoice_invoice_id_seq'::regclass);


--
-- Name: pathology_order order_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_order ALTER COLUMN order_id SET DEFAULT nextval('public.pathology_order_order_id_seq'::regclass);


--
-- Name: pathology_order_item order_item_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_order_item ALTER COLUMN order_item_id SET DEFAULT nextval('public.pathology_order_item_order_item_id_seq'::regclass);


--
-- Name: pathology_payment payment_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_payment ALTER COLUMN payment_id SET DEFAULT nextval('public.pathology_payment_payment_id_seq'::regclass);


--
-- Name: pathology_purchase_order purchase_order_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_purchase_order ALTER COLUMN purchase_order_id SET DEFAULT nextval('public.pathology_purchase_order_purchase_order_id_seq'::regclass);


--
-- Name: pathology_reagent reagent_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_reagent ALTER COLUMN reagent_id SET DEFAULT nextval('public.pathology_reagent_reagent_id_seq'::regclass);


--
-- Name: pathology_reagent_lot lot_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_reagent_lot ALTER COLUMN lot_id SET DEFAULT nextval('public.pathology_reagent_lot_lot_id_seq'::regclass);


--
-- Name: pathology_referring_doctor doctor_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_referring_doctor ALTER COLUMN doctor_id SET DEFAULT nextval('public.pathology_referring_doctor_doctor_id_seq'::regclass);


--
-- Name: pathology_report report_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_report ALTER COLUMN report_id SET DEFAULT nextval('public.pathology_report_report_id_seq'::regclass);


--
-- Name: pathology_report_content content_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_report_content ALTER COLUMN content_id SET DEFAULT nextval('public.pathology_report_content_content_id_seq'::regclass);


--
-- Name: pathology_result result_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_result ALTER COLUMN result_id SET DEFAULT nextval('public.pathology_result_result_id_seq'::regclass);


--
-- Name: pathology_result_approval approval_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_result_approval ALTER COLUMN approval_id SET DEFAULT nextval('public.pathology_result_approval_approval_id_seq'::regclass);


--
-- Name: pathology_sample sample_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_sample ALTER COLUMN sample_id SET DEFAULT nextval('public.pathology_sample_sample_id_seq'::regclass);


--
-- Name: pathology_test test_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_test ALTER COLUMN test_id SET DEFAULT nextval('public.pathology_test_test_id_seq'::regclass);


--
-- Name: pathology_test_category category_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_test_category ALTER COLUMN category_id SET DEFAULT nextval('public.pathology_test_category_category_id_seq'::regclass);


--
-- Name: pathology_test_package package_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_test_package ALTER COLUMN package_id SET DEFAULT nextval('public.pathology_test_package_package_id_seq'::regclass);


--
-- Name: pathology_test_package_item package_item_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_test_package_item ALTER COLUMN package_item_id SET DEFAULT nextval('public.pathology_test_package_item_package_item_id_seq'::regclass);


--
-- Name: dhis2_diseases DHIS2DISEASES_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.dhis2_diseases
    ADD CONSTRAINT "DHIS2DISEASES_pkey" PRIMARY KEY (dhis2_code);


--
-- Name: kmu_chart_log KMUCHARTLOG_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_chart_log
    ADD CONSTRAINT "KMUCHARTLOG_pkey" PRIMARY KEY (log_id);


--
-- Name: kmu_chart KMUCHART_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_chart
    ADD CONSTRAINT "KMUCHART_pkey" PRIMARY KEY (chr_health_id);


--
-- Name: kmu_icd KMUICD_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_icd
    ADD CONSTRAINT "KMUICD_pkey" PRIMARY KEY (icd_code);


--
-- Name: kmu_medicine KMUMEDICINE_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_medicine
    ADD CONSTRAINT "KMUMEDICINE_pkey" PRIMARY KEY (med_id);


--
-- Name: AspNetUsers PK_AspNetUsers; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AspNetUsers"
    ADD CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id");


--
-- Name: BloodUnits PK_BloodUnits; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."BloodUnits"
    ADD CONSTRAINT "PK_BloodUnits" PRIMARY KEY (id);


--
-- Name: DonorProfiles PK_DonorProfiles; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."DonorProfiles"
    ADD CONSTRAINT "PK_DonorProfiles" PRIMARY KEY ("DonorId");


--
-- Name: KMU_MergeHistory PK_KMU_MergeHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."KMU_MergeHistory"
    ADD CONSTRAINT "PK_KMU_MergeHistory" PRIMARY KEY ("Id");


--
-- Name: LedgerActions PK_LedgerActions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LedgerActions"
    ADD CONSTRAINT "PK_LedgerActions" PRIMARY KEY ("ActionId");


--
-- Name: PatientRequests PK_PatientRequests; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PatientRequests"
    ADD CONSTRAINT "PK_PatientRequests" PRIMARY KEY ("RequestId");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: home_physicalsign PK_home_physicalsign; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.home_physicalsign
    ADD CONSTRAINT "PK_home_physicalsign" PRIMARY KEY (phyid);


--
-- Name: kmu_ncd PK_kmu_Ncd; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_ncd
    ADD CONSTRAINT "PK_kmu_Ncd" PRIMARY KEY (ncdid);


--
-- Name: kmu_chart_MergeHistory PK_kmu_chart_MergeHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."kmu_chart_MergeHistory"
    ADD CONSTRAINT "PK_kmu_chart_MergeHistory" PRIMARY KEY ("Id");


--
-- Name: kmu_mental PK_kmu_mental; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_mental
    ADD CONSTRAINT "PK_kmu_mental" PRIMARY KEY (mntid);


--
-- Name: testresults PK_testresults; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.testresults
    ADD CONSTRAINT "PK_testresults" PRIMARY KEY (testresultid);


--
-- Name: beds bed_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.beds
    ADD CONSTRAINT bed_pkey PRIMARY KEY (bedid);


--
-- Name: blood_audit_logs blood_audit_logs_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_audit_logs
    ADD CONSTRAINT blood_audit_logs_pkey PRIMARY KEY (id);


--
-- Name: blood_bank_blood_unit blood_bank_blood_unit_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_blood_unit
    ADD CONSTRAINT blood_bank_blood_unit_pkey PRIMARY KEY (unit_id);


--
-- Name: blood_bank_dispense blood_bank_dispense_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_dispense
    ADD CONSTRAINT blood_bank_dispense_pkey PRIMARY KEY (dispense_id);


--
-- Name: blood_bank_donor blood_bank_donor_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_donor
    ADD CONSTRAINT blood_bank_donor_pkey PRIMARY KEY (donor_id);


--
-- Name: blood_bank_patient_event blood_bank_patient_event_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_patient_event
    ADD CONSTRAINT blood_bank_patient_event_pkey PRIMARY KEY (event_id);


--
-- Name: blood_bank_request blood_bank_request_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_request
    ADD CONSTRAINT blood_bank_request_pkey PRIMARY KEY (request_id);


--
-- Name: blood_bank_screening blood_bank_screening_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_screening
    ADD CONSTRAINT blood_bank_screening_pkey PRIMARY KEY (screening_id);


--
-- Name: blood_cbc_results blood_cbc_results_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_cbc_results
    ADD CONSTRAINT blood_cbc_results_pkey PRIMARY KEY (id);


--
-- Name: blood_doctor_requests blood_doctor_requests_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_doctor_requests
    ADD CONSTRAINT blood_doctor_requests_pkey PRIMARY KEY (id);


--
-- Name: blood_units blood_units_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_units
    ADD CONSTRAINT blood_units_pkey PRIMARY KEY (id);


--
-- Name: clinic_schedule clinic_schedule_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.clinic_schedule
    ADD CONSTRAINT clinic_schedule_pkey PRIMARY KEY (sche_week, sche_noon, sche_room, shift);


--
-- Name: hisorderplan_attr hisorderplan_attr_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.hisorderplan_attr
    ADD CONSTRAINT hisorderplan_attr_pkey PRIMARY KEY (orderplanatrrid);


--
-- Name: hisorderplan hisorderplan_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.hisorderplan
    ADD CONSTRAINT hisorderplan_pkey PRIMARY KEY (orderplanid);


--
-- Name: hisordersoa hisorderplan_soa_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.hisordersoa
    ADD CONSTRAINT hisorderplan_soa_pkey PRIMARY KEY (soaid);


--
-- Name: kmu_attribute kmu_attribute_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_attribute
    ADD CONSTRAINT kmu_attribute_pkey PRIMARY KEY (attr_code);


--
-- Name: kmu_auths_log kmu_auths_log_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_auths_log
    ADD CONSTRAINT kmu_auths_log_pkey PRIMARY KEY (edit_user, edit_time, user_idno, project_id);


--
-- Name: kmu_auths kmu_auths_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_auths
    ADD CONSTRAINT kmu_auths_pkey PRIMARY KEY (user_idno, project_id);


--
-- Name: kmu_coderef kmu_coderef_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_coderef
    ADD CONSTRAINT kmu_coderef_pkey PRIMARY KEY (ref_id);


--
-- Name: kmu_condition kmu_condition_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_condition
    ADD CONSTRAINT kmu_condition_pkey PRIMARY KEY (cnd_codetype, cnd_code);


--
-- Name: kmu_department kmu_department_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_department
    ADD CONSTRAINT kmu_department_pkey PRIMARY KEY (dpt_code);


--
-- Name: kmu_medfrequency_ind kmu_medfrequency_ind_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_medfrequency_ind
    ADD CONSTRAINT kmu_medfrequency_ind_pkey PRIMARY KEY (frq_code, ind_code);


--
-- Name: kmu_medfrequency kmu_medfrequency_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_medfrequency
    ADD CONSTRAINT kmu_medfrequency_pkey PRIMARY KEY (frq_code);


--
-- Name: kmu_medpathway kmu_medpathway_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_medpathway
    ADD CONSTRAINT kmu_medpathway_pkey PRIMARY KEY (med_type, path_code);


--
-- Name: kmu_non_medicine kmu_non_medicine_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_non_medicine
    ADD CONSTRAINT kmu_non_medicine_pkey PRIMARY KEY (item_id);


--
-- Name: kmu_projects kmu_projects_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_projects
    ADD CONSTRAINT kmu_projects_pkey PRIMARY KEY (project_id);


--
-- Name: kmu_serialpool kmu_serialpool_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_serialpool
    ADD CONSTRAINT kmu_serialpool_pkey PRIMARY KEY (serial_owner);


--
-- Name: kmu_upload_log kmu_upload_log_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_upload_log
    ADD CONSTRAINT kmu_upload_log_pkey PRIMARY KEY (logid);


--
-- Name: kmu_users_log kmu_users_log_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_users_log
    ADD CONSTRAINT kmu_users_log_pkey PRIMARY KEY (user_idno, event_type, event_time, ip);


--
-- Name: kmu_users kmu_users_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_users
    ADD CONSTRAINT kmu_users_pkey PRIMARY KEY (user_idno);


--
-- Name: medical_administration medical_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.medical_administration
    ADD CONSTRAINT medical_pkey PRIMARY KEY (id);


--
-- Name: pathology_accession_sequence pathology_accession_sequence_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_accession_sequence
    ADD CONSTRAINT pathology_accession_sequence_pkey PRIMARY KEY (section_code, sequence_date);


--
-- Name: pathology_audit_log pathology_audit_log_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_audit_log
    ADD CONSTRAINT pathology_audit_log_pkey PRIMARY KEY (audit_id);


--
-- Name: pathology_invoice pathology_invoice_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_invoice
    ADD CONSTRAINT pathology_invoice_pkey PRIMARY KEY (invoice_id);


--
-- Name: pathology_order_item pathology_order_item_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_order_item
    ADD CONSTRAINT pathology_order_item_pkey PRIMARY KEY (order_item_id);


--
-- Name: pathology_order pathology_order_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_order
    ADD CONSTRAINT pathology_order_pkey PRIMARY KEY (order_id);


--
-- Name: pathology_payment pathology_payment_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_payment
    ADD CONSTRAINT pathology_payment_pkey PRIMARY KEY (payment_id);


--
-- Name: pathology_purchase_order pathology_purchase_order_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_purchase_order
    ADD CONSTRAINT pathology_purchase_order_pkey PRIMARY KEY (purchase_order_id);


--
-- Name: pathology_reagent_lot pathology_reagent_lot_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_reagent_lot
    ADD CONSTRAINT pathology_reagent_lot_pkey PRIMARY KEY (lot_id);


--
-- Name: pathology_reagent pathology_reagent_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_reagent
    ADD CONSTRAINT pathology_reagent_pkey PRIMARY KEY (reagent_id);


--
-- Name: pathology_referring_doctor pathology_referring_doctor_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_referring_doctor
    ADD CONSTRAINT pathology_referring_doctor_pkey PRIMARY KEY (doctor_id);


--
-- Name: pathology_report_content pathology_report_content_order_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_report_content
    ADD CONSTRAINT pathology_report_content_order_id_key UNIQUE (order_id);


--
-- Name: pathology_report_content pathology_report_content_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_report_content
    ADD CONSTRAINT pathology_report_content_pkey PRIMARY KEY (content_id);


--
-- Name: pathology_report pathology_report_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_report
    ADD CONSTRAINT pathology_report_pkey PRIMARY KEY (report_id);


--
-- Name: pathology_report_template pathology_report_template_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_report_template
    ADD CONSTRAINT pathology_report_template_pkey PRIMARY KEY (template_code);


--
-- Name: pathology_result_approval pathology_result_approval_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_result_approval
    ADD CONSTRAINT pathology_result_approval_pkey PRIMARY KEY (approval_id);


--
-- Name: pathology_result pathology_result_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_result
    ADD CONSTRAINT pathology_result_pkey PRIMARY KEY (result_id);


--
-- Name: pathology_sample pathology_sample_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_sample
    ADD CONSTRAINT pathology_sample_pkey PRIMARY KEY (sample_id);


--
-- Name: pathology_test_category pathology_test_category_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_test_category
    ADD CONSTRAINT pathology_test_category_pkey PRIMARY KEY (category_id);


--
-- Name: pathology_test_package_item pathology_test_package_item_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_test_package_item
    ADD CONSTRAINT pathology_test_package_item_pkey PRIMARY KEY (package_item_id);


--
-- Name: pathology_test_package pathology_test_package_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_test_package
    ADD CONSTRAINT pathology_test_package_pkey PRIMARY KEY (package_id);


--
-- Name: pathology_test pathology_test_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_test
    ADD CONSTRAINT pathology_test_pkey PRIMARY KEY (test_id);


--
-- Name: physical_sign physical_sign_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.physical_sign
    ADD CONSTRAINT physical_sign_pkey PRIMARY KEY (phy_id);


--
-- Name: radiology_report_audit_trails radiology_report_audit_trails_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.radiology_report_audit_trails
    ADD CONSTRAINT radiology_report_audit_trails_pkey PRIMARY KEY (audit_id);


--
-- Name: radiologyexamrequests radiologyexamrequests_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.radiologyexamrequests
    ADD CONSTRAINT radiologyexamrequests_pkey PRIMARY KEY (id);


--
-- Name: radiologyreport_versions radiologyreport_versions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.radiologyreport_versions
    ADD CONSTRAINT radiologyreport_versions_pkey PRIMARY KEY (version_id);


--
-- Name: radiologyreports radiologyreports_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.radiologyreports
    ADD CONSTRAINT radiologyreports_pkey PRIMARY KEY (reportid);


--
-- Name: registration registration_inhospid_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.registration
    ADD CONSTRAINT registration_inhospid_unique UNIQUE (inhospid);


--
-- Name: registration registration_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.registration
    ADD CONSTRAINT registration_pkey PRIMARY KEY (reg_date, reg_department, reg_noon, reg_seq_no);


--
-- Name: inptient_reservation reservation_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inptient_reservation
    ADD CONSTRAINT reservation_pkey PRIMARY KEY (inhospid, reservation_date, healthid);


--
-- Name: rooms rooms_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.rooms
    ADD CONSTRAINT rooms_pkey PRIMARY KEY (roomid);


--
-- Name: transaction_call transaction_call_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.transaction_call
    ADD CONSTRAINT transaction_call_pkey PRIMARY KEY (call_id);


--
-- Name: transaction_fee transaction_fee_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.transaction_fee
    ADD CONSTRAINT transaction_fee_pkey PRIMARY KEY (transation_id);


--
-- Name: wards ward_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.wards
    ADD CONSTRAINT ward_pkey PRIMARY KEY (wardid);


--
-- Name: EmailIndex; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "EmailIndex" ON public."AspNetUsers" USING btree ("NormalizedEmail");


--
-- Name: IX_DonorProfiles_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_DonorProfiles_UserId" ON public."DonorProfiles" USING btree ("UserId");


--
-- Name: IX_LedgerActions_PerformedByUserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_LedgerActions_PerformedByUserId" ON public."LedgerActions" USING btree ("PerformedByUserId");


--
-- Name: IX_blood_bank_blood_unit_donor_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_blood_bank_blood_unit_donor_id" ON public.blood_bank_blood_unit USING btree (donor_id);


--
-- Name: IX_blood_bank_blood_unit_request_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_blood_bank_blood_unit_request_id" ON public.blood_bank_blood_unit USING btree (request_id);


--
-- Name: IX_blood_bank_blood_unit_serial_number; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_blood_bank_blood_unit_serial_number" ON public.blood_bank_blood_unit USING btree (serial_number);


--
-- Name: IX_blood_bank_dispense_request_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_blood_bank_dispense_request_id" ON public.blood_bank_dispense USING btree (request_id);


--
-- Name: IX_blood_bank_dispense_unit_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_blood_bank_dispense_unit_id" ON public.blood_bank_dispense USING btree (unit_id);


--
-- Name: IX_blood_bank_screening_unit_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_blood_bank_screening_unit_id" ON public.blood_bank_screening USING btree (unit_id);


--
-- Name: IX_pathology_audit_log_created_at; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_audit_log_created_at" ON public.pathology_audit_log USING btree (created_at);


--
-- Name: IX_pathology_invoice_invoice_no; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_pathology_invoice_invoice_no" ON public.pathology_invoice USING btree (invoice_no);


--
-- Name: IX_pathology_invoice_order_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_invoice_order_id" ON public.pathology_invoice USING btree (order_id);


--
-- Name: IX_pathology_order_accession_no; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_pathology_order_accession_no" ON public.pathology_order USING btree (accession_no);


--
-- Name: IX_pathology_order_item_order_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_order_item_order_id" ON public.pathology_order_item USING btree (order_id);


--
-- Name: IX_pathology_order_item_test_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_order_item_test_id" ON public.pathology_order_item USING btree (test_id);


--
-- Name: IX_pathology_order_orderplanid; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_order_orderplanid" ON public.pathology_order USING btree (orderplanid);


--
-- Name: IX_pathology_order_referring_doctor_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_order_referring_doctor_id" ON public.pathology_order USING btree (referring_doctor_id);


--
-- Name: IX_pathology_order_report_template_code; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_order_report_template_code" ON public.pathology_order USING btree (report_template_code);


--
-- Name: IX_pathology_order_section_code; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_order_section_code" ON public.pathology_order USING btree (section_code);


--
-- Name: IX_pathology_payment_invoice_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_payment_invoice_id" ON public.pathology_payment USING btree (invoice_id);


--
-- Name: IX_pathology_purchase_order_po_no; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_pathology_purchase_order_po_no" ON public.pathology_purchase_order USING btree (po_no);


--
-- Name: IX_pathology_reagent_lot_reagent_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_reagent_lot_reagent_id" ON public.pathology_reagent_lot USING btree (reagent_id);


--
-- Name: IX_pathology_reagent_sku; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_pathology_reagent_sku" ON public.pathology_reagent USING btree (sku);


--
-- Name: IX_pathology_referring_doctor_code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_pathology_referring_doctor_code" ON public.pathology_referring_doctor USING btree (code);


--
-- Name: IX_pathology_report_content_template; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_report_content_template" ON public.pathology_report_content USING btree (report_template_code);


--
-- Name: IX_pathology_report_order_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_report_order_id" ON public.pathology_report USING btree (order_id);


--
-- Name: IX_pathology_report_report_no; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_pathology_report_report_no" ON public.pathology_report USING btree (report_no);


--
-- Name: IX_pathology_result_approval_result_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_result_approval_result_id" ON public.pathology_result_approval USING btree (result_id);


--
-- Name: IX_pathology_result_order_item_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_result_order_item_id" ON public.pathology_result USING btree (order_item_id);


--
-- Name: IX_pathology_result_sample_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_result_sample_id" ON public.pathology_result USING btree (sample_id);


--
-- Name: IX_pathology_sample_barcode; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_pathology_sample_barcode" ON public.pathology_sample USING btree (barcode);


--
-- Name: IX_pathology_sample_order_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_sample_order_id" ON public.pathology_sample USING btree (order_id);


--
-- Name: IX_pathology_test_category_code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_pathology_test_category_code" ON public.pathology_test_category USING btree (code);


--
-- Name: IX_pathology_test_category_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_test_category_id" ON public.pathology_test USING btree (category_id);


--
-- Name: IX_pathology_test_code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_pathology_test_code" ON public.pathology_test USING btree (code);


--
-- Name: IX_pathology_test_package_code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_pathology_test_package_code" ON public.pathology_test_package USING btree (code);


--
-- Name: IX_pathology_test_package_item_package_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_test_package_item_package_id" ON public.pathology_test_package_item USING btree (package_id);


--
-- Name: IX_pathology_test_package_item_test_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_pathology_test_package_item_test_id" ON public.pathology_test_package_item USING btree (test_id);


--
-- Name: IX_radiology_report_audit_trails_report_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_radiology_report_audit_trails_report_id" ON public.radiology_report_audit_trails USING btree (report_id);


--
-- Name: IX_radiologyreport_versions_report_id_version_no; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_radiologyreport_versions_report_id_version_no" ON public.radiologyreport_versions USING btree (report_id, version_no);


--
-- Name: IX_radiologyreports_radiologyexamrequestid; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_radiologyreports_radiologyexamrequestid" ON public.radiologyreports USING btree (radiologyexamrequestid);


--
-- Name: UserNameIndex; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "UserNameIndex" ON public."AspNetUsers" USING btree ("NormalizedUserName");


--
-- Name: idx_hisorderplan_01; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_hisorderplan_01 ON public.hisorderplan USING btree (health_id COLLATE "C");


--
-- Name: idx_hisorderplan_02; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_hisorderplan_02 ON public.hisorderplan USING btree (inhospid COLLATE "C" bpchar_pattern_ops);


--
-- Name: idx_hisordersoa_01; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_hisordersoa_01 ON public.hisordersoa USING btree (inhospid COLLATE "C" bpchar_pattern_ops);


--
-- Name: idx_hisordersoa_02; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_hisordersoa_02 ON public.hisordersoa USING btree (health_id COLLATE "C" bpchar_pattern_ops);


--
-- Name: idx_icd_01; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_icd_01 ON public.kmu_icd USING btree (parent_code varchar_ops);


--
-- Name: idx_inhospid; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_inhospid ON public.kmu_upload_log USING btree (inhospid);


--
-- Name: idx_kmu_chart_01; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_kmu_chart_01 ON public.kmu_chart USING btree (chr_patient_firstname, chr_patient_midname, chr_patient_lastname);


--
-- Name: idx_kmu_chart_02; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_kmu_chart_02 ON public.kmu_chart USING btree (chr_mobile_phone);


--
-- Name: idx_kmu_chart_log_01; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_kmu_chart_log_01 ON public.kmu_chart_log USING btree (chr_health_id);


--
-- Name: idx_physical_sign_01; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_physical_sign_01 ON public.physical_sign USING btree (inhospid, phy_type);


--
-- Name: idx_registration_01; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_registration_01 ON public.registration USING btree (inhospid);


--
-- Name: idx_result_other_cols1; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_result_other_cols1 ON public.kmu_upload_log USING btree (exec_datetime, result_success);


--
-- Name: idx_result_other_cols2; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_result_other_cols2 ON public.kmu_upload_log USING btree (reg_date, result_success);


--
-- Name: idx_result_success; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_result_success ON public.kmu_upload_log USING btree (result_success);


--
-- Name: ix_radiology_report_audit_payload_gin; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_radiology_report_audit_payload_gin ON public.radiology_report_audit_trails USING gin (payload);


--
-- Name: ix_radiologyexamrequests_ordereddate; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_radiologyexamrequests_ordereddate ON public.radiologyexamrequests USING btree (ordereddate);


--
-- Name: ix_radiologyexamrequests_orderid; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_radiologyexamrequests_orderid ON public.radiologyexamrequests USING btree (orderid);


--
-- Name: ix_radiologyexamrequests_patientid; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_radiologyexamrequests_patientid ON public.radiologyexamrequests USING btree (patientid);


--
-- Name: ix_radiologyexamrequests_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_radiologyexamrequests_status ON public.radiologyexamrequests USING btree (status);


--
-- Name: ix_radiologyreports_radrequestid; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_radiologyreports_radrequestid ON public.radiologyreports USING btree (radrequestid);


--
-- Name: ix_radiologyreports_structured_findings_gin; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_radiologyreports_structured_findings_gin ON public.radiologyreports USING gin (structured_findings);


--
-- Name: beds tr_bedid; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER tr_bedid BEFORE INSERT ON public.beds FOR EACH ROW EXECUTE FUNCTION public.tr_bedid();


--
-- Name: kmu_coderef tr_coderef; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER tr_coderef BEFORE INSERT ON public.kmu_coderef FOR EACH ROW EXECUTE FUNCTION public.tr_coderef();


--
-- Name: hisorderplan tr_hisorderplan; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER tr_hisorderplan BEFORE INSERT ON public.hisorderplan FOR EACH ROW EXECUTE FUNCTION public.tr_hisorderplan();


--
-- Name: hisordersoa tr_hisordersoa; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER tr_hisordersoa BEFORE INSERT ON public.hisordersoa FOR EACH ROW EXECUTE FUNCTION public.tr_hisordersoa();


--
-- Name: registration tr_inhospid; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER tr_inhospid BEFORE INSERT ON public.registration FOR EACH ROW EXECUTE FUNCTION public.tr_inhospid();


--
-- Name: inptient_reservation tr_inpatientid; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER tr_inpatientid BEFORE INSERT ON public.inptient_reservation FOR EACH ROW EXECUTE FUNCTION public.tr_inpatientid();


--
-- Name: kmu_upload_log tr_kmu_upload_log; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER tr_kmu_upload_log BEFORE INSERT ON public.kmu_upload_log FOR EACH ROW EXECUTE FUNCTION public.tr_kmu_upload_log();


--
-- Name: kmu_chart_log tr_kmuchartlogid; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER tr_kmuchartlogid BEFORE INSERT ON public.kmu_chart_log FOR EACH ROW EXECUTE FUNCTION public.tr_kmuchartlogid();


--
-- Name: physical_sign tr_physical; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER tr_physical BEFORE INSERT ON public.physical_sign FOR EACH ROW EXECUTE FUNCTION public.tr_physical();


--
-- Name: wards tr_wardid; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER tr_wardid BEFORE INSERT ON public.wards FOR EACH ROW EXECUTE FUNCTION public.tr_wardid();


--
-- Name: DonorProfiles FK_DonorProfiles_AspNetUsers_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."DonorProfiles"
    ADD CONSTRAINT "FK_DonorProfiles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES public."AspNetUsers"("Id") ON DELETE CASCADE;


--
-- Name: LedgerActions FK_LedgerActions_AspNetUsers_PerformedByUserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LedgerActions"
    ADD CONSTRAINT "FK_LedgerActions_AspNetUsers_PerformedByUserId" FOREIGN KEY ("PerformedByUserId") REFERENCES public."AspNetUsers"("Id") ON DELETE RESTRICT;


--
-- Name: blood_bank_blood_unit FK_blood_bank_blood_unit_blood_bank_donor_donor_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_blood_unit
    ADD CONSTRAINT "FK_blood_bank_blood_unit_blood_bank_donor_donor_id" FOREIGN KEY (donor_id) REFERENCES public.blood_bank_donor(donor_id) ON DELETE CASCADE;


--
-- Name: blood_bank_blood_unit FK_blood_bank_blood_unit_blood_bank_request_request_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_blood_unit
    ADD CONSTRAINT "FK_blood_bank_blood_unit_blood_bank_request_request_id" FOREIGN KEY (request_id) REFERENCES public.blood_bank_request(request_id);


--
-- Name: blood_bank_dispense FK_blood_bank_dispense_blood_bank_blood_unit_unit_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_dispense
    ADD CONSTRAINT "FK_blood_bank_dispense_blood_bank_blood_unit_unit_id" FOREIGN KEY (unit_id) REFERENCES public.blood_bank_blood_unit(unit_id) ON DELETE CASCADE;


--
-- Name: blood_bank_dispense FK_blood_bank_dispense_blood_bank_request_request_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_dispense
    ADD CONSTRAINT "FK_blood_bank_dispense_blood_bank_request_request_id" FOREIGN KEY (request_id) REFERENCES public.blood_bank_request(request_id);


--
-- Name: blood_bank_screening FK_blood_bank_screening_blood_bank_blood_unit_unit_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.blood_bank_screening
    ADD CONSTRAINT "FK_blood_bank_screening_blood_bank_blood_unit_unit_id" FOREIGN KEY (unit_id) REFERENCES public.blood_bank_blood_unit(unit_id) ON DELETE CASCADE;


--
-- Name: radiology_report_audit_trails FK_radiology_report_audit_trails_radiologyreports_report_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.radiology_report_audit_trails
    ADD CONSTRAINT "FK_radiology_report_audit_trails_radiologyreports_report_id" FOREIGN KEY (report_id) REFERENCES public.radiologyreports(reportid) ON DELETE CASCADE;


--
-- Name: radiologyreport_versions FK_radiologyreport_versions_radiologyreports_report_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.radiologyreport_versions
    ADD CONSTRAINT "FK_radiologyreport_versions_radiologyreports_report_id" FOREIGN KEY (report_id) REFERENCES public.radiologyreports(reportid) ON DELETE CASCADE;


--
-- Name: radiologyreports FK_radiologyreports_radiologyexamrequests_radiologyexamrequest~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.radiologyreports
    ADD CONSTRAINT "FK_radiologyreports_radiologyexamrequests_radiologyexamrequest~" FOREIGN KEY (radiologyexamrequestid) REFERENCES public.radiologyexamrequests(id);


--
-- Name: hisorderplan_attr hisorderplan_attr_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.hisorderplan_attr
    ADD CONSTRAINT hisorderplan_attr_fkey FOREIGN KEY (orderplanid) REFERENCES public.hisorderplan(orderplanid) NOT VALID;


--
-- Name: kmu_auths kmu_auths_fk01; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kmu_auths
    ADD CONSTRAINT kmu_auths_fk01 FOREIGN KEY (user_idno) REFERENCES public.kmu_users(user_idno) NOT VALID;


--
-- Name: pathology_invoice pathology_invoice_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_invoice
    ADD CONSTRAINT pathology_invoice_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.pathology_order(order_id) ON DELETE CASCADE;


--
-- Name: pathology_order_item pathology_order_item_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_order_item
    ADD CONSTRAINT pathology_order_item_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.pathology_order(order_id) ON DELETE CASCADE;


--
-- Name: pathology_order_item pathology_order_item_test_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_order_item
    ADD CONSTRAINT pathology_order_item_test_id_fkey FOREIGN KEY (test_id) REFERENCES public.pathology_test(test_id);


--
-- Name: pathology_order pathology_order_referring_doctor_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_order
    ADD CONSTRAINT pathology_order_referring_doctor_id_fkey FOREIGN KEY (referring_doctor_id) REFERENCES public.pathology_referring_doctor(doctor_id);


--
-- Name: pathology_payment pathology_payment_invoice_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_payment
    ADD CONSTRAINT pathology_payment_invoice_id_fkey FOREIGN KEY (invoice_id) REFERENCES public.pathology_invoice(invoice_id) ON DELETE CASCADE;


--
-- Name: pathology_reagent_lot pathology_reagent_lot_reagent_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_reagent_lot
    ADD CONSTRAINT pathology_reagent_lot_reagent_id_fkey FOREIGN KEY (reagent_id) REFERENCES public.pathology_reagent(reagent_id) ON DELETE CASCADE;


--
-- Name: pathology_report_content pathology_report_content_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_report_content
    ADD CONSTRAINT pathology_report_content_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.pathology_order(order_id) ON DELETE CASCADE;


--
-- Name: pathology_report pathology_report_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_report
    ADD CONSTRAINT pathology_report_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.pathology_order(order_id) ON DELETE CASCADE;


--
-- Name: pathology_result_approval pathology_result_approval_result_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_result_approval
    ADD CONSTRAINT pathology_result_approval_result_id_fkey FOREIGN KEY (result_id) REFERENCES public.pathology_result(result_id) ON DELETE CASCADE;


--
-- Name: pathology_result pathology_result_order_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_result
    ADD CONSTRAINT pathology_result_order_item_id_fkey FOREIGN KEY (order_item_id) REFERENCES public.pathology_order_item(order_item_id) ON DELETE CASCADE;


--
-- Name: pathology_result pathology_result_sample_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_result
    ADD CONSTRAINT pathology_result_sample_id_fkey FOREIGN KEY (sample_id) REFERENCES public.pathology_sample(sample_id);


--
-- Name: pathology_sample pathology_sample_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_sample
    ADD CONSTRAINT pathology_sample_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.pathology_order(order_id) ON DELETE CASCADE;


--
-- Name: pathology_test pathology_test_category_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_test
    ADD CONSTRAINT pathology_test_category_id_fkey FOREIGN KEY (category_id) REFERENCES public.pathology_test_category(category_id) ON DELETE CASCADE;


--
-- Name: pathology_test_package_item pathology_test_package_item_package_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_test_package_item
    ADD CONSTRAINT pathology_test_package_item_package_id_fkey FOREIGN KEY (package_id) REFERENCES public.pathology_test_package(package_id) ON DELETE CASCADE;


--
-- Name: pathology_test_package_item pathology_test_package_item_test_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pathology_test_package_item
    ADD CONSTRAINT pathology_test_package_item_test_id_fkey FOREIGN KEY (test_id) REFERENCES public.pathology_test(test_id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

