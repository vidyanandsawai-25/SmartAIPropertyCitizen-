USE NTIS_4021_T1;
GO

IF OBJECT_ID('dbo.Rpt_OwnerHeadWiseTaxDetails', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Rpt_OwnerHeadWiseTaxDetails;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE PROC dbo.Rpt_OwnerHeadWiseTaxDetails
(
    @OwnerID INT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FinanceYear INT, @PendingYear INT;
    
    SELECT @FinanceYear = ISNULL(MAX(FinanceYear),0)
    FROM TransYearMast WITH (NOLOCK);

    SET @PendingYear = @FinanceYear - 1;

    CREATE TABLE #Result
    (
        TaxNameMarathi NVARCHAR(200),
        TaxName VARCHAR(100),
        PendingAmount FLOAT,
        CurrentAmount FLOAT,
        DisplayOrder INT
    );

    -- Insert demand details
    INSERT INTO #Result (TaxNameMarathi, TaxName, PendingAmount, CurrentAmount, DisplayOrder)
    SELECT 
        TM.MarathiTaxName,
        TM.TaxName,
        SUM(CASE WHEN FB.FinanceYear <= @PendingYear THEN FB.BalanceAmount ELSE 0 END),
        SUM(CASE WHEN FB.FinanceYear = @FinanceYear THEN FB.BalanceAmount ELSE 0 END),
        TM.DisplayOrder
    FROM FinalBalance FB WITH (NOLOCK)
    JOIN TaxMast TM WITH (NOLOCK) ON FB.TaxID = TM.TaxID
    WHERE FB.OwnerID = @OwnerID
    GROUP BY TM.MarathiTaxName, TM.TaxName, TM.DisplayOrder;

    -- Return the result
    SELECT 
        TaxNameMarathi AS [कराचे नाव],
        PendingAmount AS [मागील बाकी रु.],
        CurrentAmount AS [चालू बाकी रु.],
        (PendingAmount + CurrentAmount) AS [एकूण मागणी रु.]
    FROM #Result
    WHERE (PendingAmount + CurrentAmount) > 0
    ORDER BY DisplayOrder;

    DROP TABLE #Result;
END;
GO
