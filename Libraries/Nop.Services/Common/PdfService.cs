﻿// RTL Support provided by Credo inc (www.credo.co.il  ||   info@credo.co.il)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Core.Html;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;

namespace Nop.Services.Common
{
    /// <summary>
    /// PDF service
    /// </summary>
    public partial class PdfService : IPdfService
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ILanguageService _languageService;
        private readonly IWorkContext _workContext;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ICurrencyService _currencyService;
        private readonly IMeasureService _measureService;
        private readonly IPictureService _pictureService;
        private readonly IProductService _productService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IStoreService _storeService;
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly IAddressAttributeFormatter _addressAttributeFormatter;
        private readonly CatalogSettings _catalogSettings;
        private readonly CurrencySettings _currencySettings;
        private readonly MeasureSettings _measureSettings;
        private readonly PdfSettings _pdfSettings;
        private readonly TaxSettings _taxSettings;
        private readonly AddressSettings _addressSettings;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="localizationService">Localization service</param>
        /// <param name="languageService">Language service</param>
        /// <param name="workContext">Work context</param>
        /// <param name="orderService">Order service</param>
        /// <param name="paymentService">Payment service</param>
        /// <param name="dateTimeHelper">Date time helper</param>
        /// <param name="priceFormatter">Price formatter</param>
        /// <param name="currencyService">Currency service</param>
        /// <param name="measureService">Measure service</param>
        /// <param name="pictureService">Picture service</param>
        /// <param name="productService">Product service</param>
        /// <param name="productAttributeParser">Product attribute parser</param>
        /// <param name="storeService">Store service</param>
        /// <param name="storeContext">Store context</param>
        /// <param name="settingService">Setting service</param>
        /// <param name="addressAttributeFormatter">Address attribute formatter</param>
        /// <param name="catalogSettings">Catalog settings</param>
        /// <param name="currencySettings">Currency settings</param>
        /// <param name="measureSettings">Measure settings</param>
        /// <param name="pdfSettings">PDF sSettings</param>
        /// <param name="taxSettings">Tax settings</param>
        /// <param name="addressSettings">Address settings</param>
        public PdfService(ILocalizationService localizationService, 
            ILanguageService languageService,
            IWorkContext workContext,
            IOrderService orderService,
            IPaymentService paymentService,
            IDateTimeHelper dateTimeHelper,
            IPriceFormatter priceFormatter,
            ICurrencyService currencyService, 
            IMeasureService measureService,
            IPictureService pictureService,
            IProductService productService, 
            IProductAttributeParser productAttributeParser,
            IStoreService storeService,
            IStoreContext storeContext,
            ISettingService settingService,
            IAddressAttributeFormatter addressAttributeFormatter,
            CatalogSettings catalogSettings, 
            CurrencySettings currencySettings,
            MeasureSettings measureSettings,
            PdfSettings pdfSettings,
            TaxSettings taxSettings,
            AddressSettings addressSettings)
        {
            this._localizationService = localizationService;
            this._languageService = languageService;
            this._workContext = workContext;
            this._orderService = orderService;
            this._paymentService = paymentService;
            this._dateTimeHelper = dateTimeHelper;
            this._priceFormatter = priceFormatter;
            this._currencyService = currencyService;
            this._measureService = measureService;
            this._pictureService = pictureService;
            this._productService = productService;
            this._productAttributeParser = productAttributeParser;
            this._storeService = storeService;
            this._storeContext = storeContext;
            this._settingService = settingService;
            this._addressAttributeFormatter = addressAttributeFormatter;
            this._currencySettings = currencySettings;
            this._catalogSettings = catalogSettings;
            this._measureSettings = measureSettings;
            this._pdfSettings = pdfSettings;
            this._taxSettings = taxSettings;
            this._addressSettings = addressSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get font
        /// </summary>
        /// <returns>Font</returns>
        protected virtual Font GetFont()
        {
            //nopCommerce supports unicode characters
            //nopCommerce uses Free Serif font by default (~/App_Data/Pdf/FreeSerif.ttf file)
            //It was downloaded from http://savannah.gnu.org/projects/freefont
            return GetFont(_pdfSettings.FontFileName);
        }
        /// <summary>
        /// Get font
        /// </summary>
        /// <param name="fontFileName">Font file name</param>
        /// <returns>Font</returns>
        protected virtual Font GetFont(string fontFileName)
        {
            if (fontFileName == null)
                throw new ArgumentNullException(nameof(fontFileName));

            var fontPath = Path.Combine(CommonHelper.MapPath("~/App_Data/Pdf/"), fontFileName);
            var baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            var font = new Font(baseFont, 10, Font.NORMAL);
            return font;
        }

        /// <summary>
        /// Get font direction
        /// </summary>
        /// <param name="lang">Language</param>
        /// <returns>Font direction</returns>
        protected virtual int GetDirection(Language lang)
        {
            return lang.Rtl ? PdfWriter.RUN_DIRECTION_RTL : PdfWriter.RUN_DIRECTION_LTR;
        }

        /// <summary>
        /// Get element alignment
        /// </summary>
        /// <param name="lang">Language</param>
        /// <param name="isOpposite">Is opposite?</param>
        /// <returns>Element alignment</returns>
        protected virtual int GetAlignment(Language lang, bool isOpposite = false)
        {
            //if we need the element to be opposite, like logo etc`.
            if (!isOpposite)
                return lang.Rtl ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT;
            
            return lang.Rtl ? Element.ALIGN_LEFT : Element.ALIGN_RIGHT;
        }

        /// <summary>
        /// Get PDF cell
        /// </summary>
        /// <param name="resourceKey">Locale</param>
        /// <param name="lang">Language</param>
        /// <param name="font">Font</param>
        /// <returns>PDF cell</returns>
        protected virtual PdfPCell GetPdfCell(string resourceKey, Language lang, Font font)
        {
            return new PdfPCell(new Phrase(_localizationService.GetResource(resourceKey, lang.Id), font));
        }

        /// <summary>
        /// Get PDF cell
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="font">Font</param>
        /// <returns>PDF cell</returns>
        protected virtual PdfPCell GetPdfCell(object text, Font font)
        {
            return new PdfPCell(new Phrase(text.ToString(), font));
        }

        /// <summary>
        /// Get paragraph
        /// </summary>
        /// <param name="resourceKey">Locale</param>
        /// <param name="lang">Language</param>
        /// <param name="font">Font</param>
        /// <param name="args">Locale arguments</param>
        /// <returns>Paragraph</returns>
        protected virtual Paragraph GetParagraph(string resourceKey, Language lang, Font font, params object[] args)
        {
            return GetParagraph(resourceKey, string.Empty, lang, font, args);
        }

        /// <summary>
        /// Get paragraph
        /// </summary>
        /// <param name="resourceKey">Locale</param>
        /// <param name="indent">Indent</param>
        /// <param name="lang">Language</param>
        /// <param name="font">Font</param>
        /// <param name="args">Locale arguments</param>
        /// <returns>Paragraph</returns>
        protected virtual Paragraph GetParagraph(string resourceKey, string indent, Language lang, Font font, params object[] args)
        {
            var formatText = _localizationService.GetResource(resourceKey, lang.Id);
            return new Paragraph(indent + (args.Any() ? string.Format(formatText, args) : formatText), font);
        }

        /// <summary>
        /// Print footer
        /// </summary>
        /// <param name="pdfSettingsByStore">PDF settings</param>
        /// <param name="pdfWriter">PDF writer</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="lang">Language</param>
        /// <param name="font">Font</param>
        protected virtual void PrintFooter(PdfSettings pdfSettingsByStore, PdfWriter pdfWriter, Rectangle pageSize, Language lang, Font font)
        {
            if (string.IsNullOrEmpty(pdfSettingsByStore.InvoiceFooterTextColumn1) && string.IsNullOrEmpty(pdfSettingsByStore.InvoiceFooterTextColumn2))
                return;

            var column1Lines = string.IsNullOrEmpty(pdfSettingsByStore.InvoiceFooterTextColumn1)
                ? new List<string>()
                : pdfSettingsByStore.InvoiceFooterTextColumn1
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            var column2Lines = string.IsNullOrEmpty(pdfSettingsByStore.InvoiceFooterTextColumn2)
                ? new List<string>()
                : pdfSettingsByStore.InvoiceFooterTextColumn2
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

            if (!column1Lines.Any() && !column2Lines.Any())
                return;

            var totalLines = Math.Max(column1Lines.Count, column2Lines.Count);
            const float margin = 43;

            //if you have really a lot of lines in the footer, then replace 9 with 10 or 11
            var footerHeight = totalLines * 9;
            var directContent = pdfWriter.DirectContent;
            directContent.MoveTo(pageSize.GetLeft(margin), pageSize.GetBottom(margin) + footerHeight);
            directContent.LineTo(pageSize.GetRight(margin), pageSize.GetBottom(margin) + footerHeight);
            directContent.Stroke();

            var footerTable = new PdfPTable(2)
            {
                WidthPercentage = 100f,
                RunDirection = GetDirection(lang)
            };
            footerTable.SetTotalWidth(new float[] { 250, 250 });

            //column 1
            if (column1Lines.Any())
            {
                var column1 = new PdfPCell(new Phrase())
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_LEFT
                };

                foreach (var footerLine in column1Lines)
                {
                    column1.Phrase.Add(new Phrase(footerLine, font));
                    column1.Phrase.Add(new Phrase(Environment.NewLine));
                }
                footerTable.AddCell(column1);
            }
            else
            {
                var column = new PdfPCell(new Phrase(" ")) {Border = Rectangle.NO_BORDER};
                footerTable.AddCell(column);
            }

            //column 2
            if (column2Lines.Any())
            {
                var column2 = new PdfPCell(new Phrase())
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_LEFT
                };

                foreach (var footerLine in column2Lines)
                {
                    column2.Phrase.Add(new Phrase(footerLine, font));
                    column2.Phrase.Add(new Phrase(Environment.NewLine));
                }
                footerTable.AddCell(column2);
            }
            else
            {
                var column = new PdfPCell(new Phrase(" ")) {Border = Rectangle.NO_BORDER};
                footerTable.AddCell(column);
            }

            footerTable.WriteSelectedRows(0, totalLines, pageSize.GetLeft(margin), pageSize.GetBottom(margin) + footerHeight, directContent);
        }

        /// <summary>
        /// Print order notes
        /// </summary>
        /// <param name="pdfSettingsByStore">PDF settings</param>
        /// <param name="order">Order</param>
        /// <param name="lang">Language</param>
        /// <param name="titleFont">Title font</param>
        /// <param name="doc">Document</param>
        /// <param name="font">Font</param>
        protected virtual void PrintOrderNotes(PdfSettings pdfSettingsByStore, Order order, Language lang, Font titleFont, Document doc, Font font)
        {
            if (!pdfSettingsByStore.RenderOrderNotes)
                return;

            var orderNotes = order.OrderNotes
                .Where(on => on.DisplayToCustomer)
                .OrderByDescending(on => on.CreatedOnUtc)
                .ToList();

            if (!orderNotes.Any())
                return;

            var notesHeader = new PdfPTable(1)
            {
                RunDirection = GetDirection(lang),
                WidthPercentage = 100f
            };

            var cellOrderNote = GetPdfCell("PDFInvoice.OrderNotes", lang, titleFont);
            cellOrderNote.Border = Rectangle.NO_BORDER;
            notesHeader.AddCell(cellOrderNote);
            doc.Add(notesHeader);
            doc.Add(new Paragraph(" "));

            var notesTable = new PdfPTable(2)
            {
                RunDirection = GetDirection(lang),
                WidthPercentage = 100f
            };
            notesTable.SetWidths(lang.Rtl ? new[] {70, 30} : new[] {30, 70});

            //created on
            cellOrderNote = GetPdfCell("PDFInvoice.OrderNotes.CreatedOn", lang, font);
            cellOrderNote.BackgroundColor = BaseColor.LIGHT_GRAY;
            cellOrderNote.HorizontalAlignment = Element.ALIGN_CENTER;
            notesTable.AddCell(cellOrderNote);

            //note
            cellOrderNote = GetPdfCell("PDFInvoice.OrderNotes.Note", lang, font);
            cellOrderNote.BackgroundColor = BaseColor.LIGHT_GRAY;
            cellOrderNote.HorizontalAlignment = Element.ALIGN_CENTER;
            notesTable.AddCell(cellOrderNote);

            foreach (var orderNote in orderNotes)
            {
                cellOrderNote = GetPdfCell(_dateTimeHelper.ConvertToUserTime(orderNote.CreatedOnUtc, DateTimeKind.Utc), font);
                cellOrderNote.HorizontalAlignment = Element.ALIGN_LEFT;
                notesTable.AddCell(cellOrderNote);

                cellOrderNote = GetPdfCell(HtmlHelper.ConvertHtmlToPlainText(orderNote.FormatOrderNoteText(), true, true), font);
                cellOrderNote.HorizontalAlignment = Element.ALIGN_LEFT;
                notesTable.AddCell(cellOrderNote);

                //should we display a link to downloadable files here?
                //I think, no. Onyway, PDFs are printable documents and links (files) are useful here
            }

            doc.Add(notesTable);
        }
        
        /// <summary>
        /// Print totals
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="lang">Language</param>
        /// <param name="order">Order</param>
        /// <param name="font">Text font</param>
        /// <param name="titleFont">Title font</param>
        /// <param name="doc">PDF document</param>
        protected virtual void PrintTotals(int vendorId, Language lang, Order order, Font font, Font titleFont, Document doc)
        {
            //vendors cannot see totals
            if (vendorId != 0)
                return;

            //subtotal
            var totalsTable = new PdfPTable(1)
            {
                RunDirection = GetDirection(lang),
                WidthPercentage = 100f
            };
            totalsTable.DefaultCell.Border = Rectangle.NO_BORDER;

            //order subtotal
            if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax &&
                !_taxSettings.ForceTaxExclusionFromOrderSubtotal)
            {
                //including tax

                var orderSubtotalInclTaxInCustomerCurrency =
                    _currencyService.ConvertCurrency(order.OrderSubtotalInclTax, order.CurrencyRate);
                var orderSubtotalInclTaxStr = _priceFormatter.FormatPrice(orderSubtotalInclTaxInCustomerCurrency, true,
                    order.CustomerCurrencyCode, lang, true);

                var p = GetPdfCell($"{_localizationService.GetResource("PDFInvoice.Sub-Total", lang.Id)} {orderSubtotalInclTaxStr}", font);
                p.HorizontalAlignment = Element.ALIGN_RIGHT;
                p.Border = Rectangle.NO_BORDER;
                totalsTable.AddCell(p);
            }
            else
            {
                //excluding tax

                var orderSubtotalExclTaxInCustomerCurrency =
                    _currencyService.ConvertCurrency(order.OrderSubtotalExclTax, order.CurrencyRate);
                var orderSubtotalExclTaxStr = _priceFormatter.FormatPrice(orderSubtotalExclTaxInCustomerCurrency, true,
                    order.CustomerCurrencyCode, lang, false);

                var p = GetPdfCell($"{_localizationService.GetResource("PDFInvoice.Sub-Total", lang.Id)} {orderSubtotalExclTaxStr}", font);
                p.HorizontalAlignment = Element.ALIGN_RIGHT;
                p.Border = Rectangle.NO_BORDER;
                totalsTable.AddCell(p);
            }

            //discount (applied to order subtotal)
            if (order.OrderSubTotalDiscountExclTax > decimal.Zero)
            {
                //order subtotal
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax &&
                    !_taxSettings.ForceTaxExclusionFromOrderSubtotal)
                {
                    //including tax

                    var orderSubTotalDiscountInclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.OrderSubTotalDiscountInclTax, order.CurrencyRate);
                    var orderSubTotalDiscountInCustomerCurrencyStr = _priceFormatter.FormatPrice(
                        -orderSubTotalDiscountInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, lang, true);

                    var p = GetPdfCell($"{_localizationService.GetResource("PDFInvoice.Discount", lang.Id)} {orderSubTotalDiscountInCustomerCurrencyStr}", font);
                    p.HorizontalAlignment = Element.ALIGN_RIGHT;
                    p.Border = Rectangle.NO_BORDER;
                    totalsTable.AddCell(p);
                }
                else
                {
                    //excluding tax

                    var orderSubTotalDiscountExclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.OrderSubTotalDiscountExclTax, order.CurrencyRate);
                    var orderSubTotalDiscountInCustomerCurrencyStr = _priceFormatter.FormatPrice(
                        -orderSubTotalDiscountExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, lang, false);

                    var p = GetPdfCell($"{_localizationService.GetResource("PDFInvoice.Discount", lang.Id)} {orderSubTotalDiscountInCustomerCurrencyStr}", font);
                    p.HorizontalAlignment = Element.ALIGN_RIGHT;
                    p.Border = Rectangle.NO_BORDER;
                    totalsTable.AddCell(p);
                }
            }

            //shipping
            if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    var orderShippingInclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.OrderShippingInclTax, order.CurrencyRate);
                    var orderShippingInclTaxStr = _priceFormatter.FormatShippingPrice(
                        orderShippingInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, lang, true);

                    var p = GetPdfCell($"{_localizationService.GetResource("PDFInvoice.Shipping", lang.Id)} {orderShippingInclTaxStr}", font);
                    p.HorizontalAlignment = Element.ALIGN_RIGHT;
                    p.Border = Rectangle.NO_BORDER;
                    totalsTable.AddCell(p);
                }
                else
                {
                    //excluding tax
                    var orderShippingExclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.OrderShippingExclTax, order.CurrencyRate);
                    var orderShippingExclTaxStr = _priceFormatter.FormatShippingPrice(
                        orderShippingExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, lang, false);

                    var p = GetPdfCell($"{_localizationService.GetResource("PDFInvoice.Shipping", lang.Id)} {orderShippingExclTaxStr}", font);
                    p.HorizontalAlignment = Element.ALIGN_RIGHT;
                    p.Border = Rectangle.NO_BORDER;
                    totalsTable.AddCell(p);
                }
            }

            //payment fee
            if (order.PaymentMethodAdditionalFeeExclTax > decimal.Zero)
            {
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    var paymentMethodAdditionalFeeInclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.PaymentMethodAdditionalFeeInclTax, order.CurrencyRate);
                    var paymentMethodAdditionalFeeInclTaxStr = _priceFormatter.FormatPaymentMethodAdditionalFee(
                        paymentMethodAdditionalFeeInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, lang, true);

                    var p = GetPdfCell($"{_localizationService.GetResource("PDFInvoice.PaymentMethodAdditionalFee", lang.Id)} {paymentMethodAdditionalFeeInclTaxStr}", font);
                    p.HorizontalAlignment = Element.ALIGN_RIGHT;
                    p.Border = Rectangle.NO_BORDER;
                    totalsTable.AddCell(p);
                }
                else
                {
                    //excluding tax
                    var paymentMethodAdditionalFeeExclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.PaymentMethodAdditionalFeeExclTax, order.CurrencyRate);
                    var paymentMethodAdditionalFeeExclTaxStr = _priceFormatter.FormatPaymentMethodAdditionalFee(
                        paymentMethodAdditionalFeeExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, lang, false);

                    var p = GetPdfCell($"{_localizationService.GetResource("PDFInvoice.PaymentMethodAdditionalFee", lang.Id)} {paymentMethodAdditionalFeeExclTaxStr}", font);
                    p.HorizontalAlignment = Element.ALIGN_RIGHT;
                    p.Border = Rectangle.NO_BORDER;
                    totalsTable.AddCell(p);
                }
            }

            //tax
            var taxStr = string.Empty;
            var taxRates = new SortedDictionary<decimal, decimal>();
            bool displayTax;
            var displayTaxRates = true;
            if (_taxSettings.HideTaxInOrderSummary && order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
            {
                displayTax = false;
            }
            else
            {
                if (order.OrderTax == 0 && _taxSettings.HideZeroTax)
                {
                    displayTax = false;
                    displayTaxRates = false;
                }
                else
                {
                    taxRates = order.TaxRatesDictionary;

                    displayTaxRates = _taxSettings.DisplayTaxRates && taxRates.Any();
                    displayTax = !displayTaxRates;

                    var orderTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTax, order.CurrencyRate);
                    taxStr = _priceFormatter.FormatPrice(orderTaxInCustomerCurrency, true, order.CustomerCurrencyCode,
                        false, lang);
                }
            }
            if (displayTax)
            {
                var p = GetPdfCell($"{_localizationService.GetResource("PDFInvoice.Tax", lang.Id)} {taxStr}", font);
                p.HorizontalAlignment = Element.ALIGN_RIGHT;
                p.Border = Rectangle.NO_BORDER;
                totalsTable.AddCell(p);
            }
            if (displayTaxRates)
            {
                foreach (var item in taxRates)
                {
                    var taxRate = string.Format(_localizationService.GetResource("PDFInvoice.TaxRate", lang.Id),
                        _priceFormatter.FormatTaxRate(item.Key));
                    var taxValue = _priceFormatter.FormatPrice(
                        _currencyService.ConvertCurrency(item.Value, order.CurrencyRate), true, order.CustomerCurrencyCode,
                        false, lang);

                    var p = GetPdfCell($"{taxRate} {taxValue}", font);
                    p.HorizontalAlignment = Element.ALIGN_RIGHT;
                    p.Border = Rectangle.NO_BORDER;
                    totalsTable.AddCell(p);
                }
            }

            //discount (applied to order total)
            if (order.OrderDiscount > decimal.Zero)
            {
                var orderDiscountInCustomerCurrency =
                    _currencyService.ConvertCurrency(order.OrderDiscount, order.CurrencyRate);
                var orderDiscountInCustomerCurrencyStr = _priceFormatter.FormatPrice(-orderDiscountInCustomerCurrency,
                    true, order.CustomerCurrencyCode, false, lang);

                var p = GetPdfCell($"{_localizationService.GetResource("PDFInvoice.Discount", lang.Id)} {orderDiscountInCustomerCurrencyStr}", font);
                p.HorizontalAlignment = Element.ALIGN_RIGHT;
                p.Border = Rectangle.NO_BORDER;
                totalsTable.AddCell(p);
            }

            //gift cards
            foreach (var gcuh in order.GiftCardUsageHistory)
            {
                var gcTitle = string.Format(_localizationService.GetResource("PDFInvoice.GiftCardInfo", lang.Id),
                    gcuh.GiftCard.GiftCardCouponCode);
                var gcAmountStr = _priceFormatter.FormatPrice(
                    -(_currencyService.ConvertCurrency(gcuh.UsedValue, order.CurrencyRate)), true,
                    order.CustomerCurrencyCode, false, lang);

                var p = GetPdfCell($"{gcTitle} {gcAmountStr}", font);
                p.HorizontalAlignment = Element.ALIGN_RIGHT;
                p.Border = Rectangle.NO_BORDER;
                totalsTable.AddCell(p);
            }

            //reward points
            if (order.RedeemedRewardPointsEntry != null)
            {
                var rpTitle = string.Format(_localizationService.GetResource("PDFInvoice.RewardPoints", lang.Id),
                    -order.RedeemedRewardPointsEntry.Points);
                var rpAmount = _priceFormatter.FormatPrice(
                    -(_currencyService.ConvertCurrency(order.RedeemedRewardPointsEntry.UsedAmount, order.CurrencyRate)),
                    true, order.CustomerCurrencyCode, false, lang);

                var p = GetPdfCell($"{rpTitle} {rpAmount}", font);
                p.HorizontalAlignment = Element.ALIGN_RIGHT;
                p.Border = Rectangle.NO_BORDER;
                totalsTable.AddCell(p);
            }

            //order total
            var orderTotalInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTotal, order.CurrencyRate);
            var orderTotalStr = _priceFormatter.FormatPrice(orderTotalInCustomerCurrency, true, order.CustomerCurrencyCode, false, lang);

            var pTotal = GetPdfCell($"{_localizationService.GetResource("PDFInvoice.OrderTotal", lang.Id)} {orderTotalStr}", titleFont);
            pTotal.HorizontalAlignment = Element.ALIGN_RIGHT;
            pTotal.Border = Rectangle.NO_BORDER;
            totalsTable.AddCell(pTotal);

            doc.Add(totalsTable);
        }

        /// <summary>
        /// Print checkout attributes
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="order">Order</param>
        /// <param name="doc">Document</param>
        /// <param name="lang">Language</param>
        /// <param name="font">Font</param>
        protected virtual void PrintCheckoutAttributes(int vendorId, Order order, Document doc, Language lang, Font font)
        {
            //vendors cannot see checkout attributes
            if (vendorId != 0 || string.IsNullOrEmpty(order.CheckoutAttributeDescription))
                return;

            doc.Add(new Paragraph(" "));
            var attribTable = new PdfPTable(1)
            {
                RunDirection = GetDirection(lang),
                WidthPercentage = 100f
            };
                
            var cCheckoutAttributes = GetPdfCell(HtmlHelper.ConvertHtmlToPlainText(order.CheckoutAttributeDescription, true, true), font);
            cCheckoutAttributes.Border = Rectangle.NO_BORDER;
            cCheckoutAttributes.HorizontalAlignment = Element.ALIGN_RIGHT;
            attribTable.AddCell(cCheckoutAttributes);
            doc.Add(attribTable);
        }

        /// <summary>
        /// Print products
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="lang">Language</param>
        /// <param name="titleFont">Title font</param>
        /// <param name="doc">Document</param>
        /// <param name="order">Order</param>
        /// <param name="font">Text font</param>
        /// <param name="attributesFont">Product attributes font</param>
        protected virtual void PrintProducts(int vendorId, Language lang, Font titleFont, Document doc, Order order, Font font, Font attributesFont)
        {
            var productsHeader = new PdfPTable(1)
            {
                RunDirection = GetDirection(lang),
                WidthPercentage = 100f
            };
            var cellProducts = GetPdfCell("PDFInvoice.Product(s)", lang, titleFont);
            cellProducts.Border = Rectangle.NO_BORDER;
            productsHeader.AddCell(cellProducts);
            doc.Add(productsHeader);
            doc.Add(new Paragraph(" "));

            var orderItems = order.OrderItems;

            var productsTable = new PdfPTable(_catalogSettings.ShowSkuOnProductDetailsPage ? 5 : 4)
            {
                RunDirection = GetDirection(lang),
                WidthPercentage = 100f
            };

            if (lang.Rtl)
            {
                productsTable.SetWidths(_catalogSettings.ShowSkuOnProductDetailsPage
                    ? new[] { 15, 10, 15, 15, 45 }
                    : new[] { 20, 10, 20, 50 });
            }
            else
            {
                productsTable.SetWidths(_catalogSettings.ShowSkuOnProductDetailsPage
                    ? new[] { 45, 15, 15, 10, 15 }
                    : new[] { 50, 20, 10, 20 });
            }

            //product name
            var cellProductItem = GetPdfCell("PDFInvoice.ProductName", lang, font);
            cellProductItem.BackgroundColor = BaseColor.LIGHT_GRAY;
            cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
            productsTable.AddCell(cellProductItem);

            //SKU
            if (_catalogSettings.ShowSkuOnProductDetailsPage)
            {
                cellProductItem = GetPdfCell("PDFInvoice.SKU", lang, font);
                cellProductItem.BackgroundColor = BaseColor.LIGHT_GRAY;
                cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
                productsTable.AddCell(cellProductItem);
            }

            //price
            cellProductItem = GetPdfCell("PDFInvoice.ProductPrice", lang, font);
            cellProductItem.BackgroundColor = BaseColor.LIGHT_GRAY;
            cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
            productsTable.AddCell(cellProductItem);

            //qty
            cellProductItem = GetPdfCell("PDFInvoice.ProductQuantity", lang, font);
            cellProductItem.BackgroundColor = BaseColor.LIGHT_GRAY;
            cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
            productsTable.AddCell(cellProductItem);

            //total
            cellProductItem = GetPdfCell("PDFInvoice.ProductTotal", lang, font);
            cellProductItem.BackgroundColor = BaseColor.LIGHT_GRAY;
            cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
            productsTable.AddCell(cellProductItem);

            foreach (var orderItem in orderItems)
            {
                var p = orderItem.Product;

                //a vendor should have access only to his products
                if (vendorId > 0 && p.VendorId != vendorId)
                    continue;

                var pAttribTable = new PdfPTable(1) {RunDirection = GetDirection(lang)};
                pAttribTable.DefaultCell.Border = Rectangle.NO_BORDER;

                //product name
                var name = p.GetLocalized(x => x.Name, lang.Id);
                pAttribTable.AddCell(new Paragraph(name, font));
                cellProductItem.AddElement(new Paragraph(name, font));
                //attributes
                if (!string.IsNullOrEmpty(orderItem.AttributeDescription))
                {
                    var attributesParagraph =
                        new Paragraph(HtmlHelper.ConvertHtmlToPlainText(orderItem.AttributeDescription, true, true),
                            attributesFont);
                    pAttribTable.AddCell(attributesParagraph);
                }
                //rental info
                if (orderItem.Product.IsRental)
                {
                    var rentalStartDate = orderItem.RentalStartDateUtc.HasValue
                        ? orderItem.Product.FormatRentalDate(orderItem.RentalStartDateUtc.Value)
                        : "";
                    var rentalEndDate = orderItem.RentalEndDateUtc.HasValue
                        ? orderItem.Product.FormatRentalDate(orderItem.RentalEndDateUtc.Value)
                        : "";
                    var rentalInfo = string.Format(_localizationService.GetResource("Order.Rental.FormattedDate"),
                        rentalStartDate, rentalEndDate);

                    var rentalInfoParagraph = new Paragraph(rentalInfo, attributesFont);
                    pAttribTable.AddCell(rentalInfoParagraph);
                }
                productsTable.AddCell(pAttribTable);

                //SKU
                if (_catalogSettings.ShowSkuOnProductDetailsPage)
                {
                    var sku = p.FormatSku(orderItem.AttributesXml, _productAttributeParser);
                    cellProductItem = GetPdfCell(sku ?? string.Empty, font);
                    cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
                    productsTable.AddCell(cellProductItem);
                }

                //price
                string unitPrice;
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    var unitPriceInclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(orderItem.UnitPriceInclTax, order.CurrencyRate);
                    unitPrice = _priceFormatter.FormatPrice(unitPriceInclTaxInCustomerCurrency, true,
                        order.CustomerCurrencyCode, lang, true);
                }
                else
                {
                    //excluding tax
                    var unitPriceExclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(orderItem.UnitPriceExclTax, order.CurrencyRate);
                    unitPrice = _priceFormatter.FormatPrice(unitPriceExclTaxInCustomerCurrency, true,
                        order.CustomerCurrencyCode, lang, false);
                }
                cellProductItem = GetPdfCell(unitPrice, font);
                cellProductItem.HorizontalAlignment = Element.ALIGN_LEFT;
                productsTable.AddCell(cellProductItem);

                //qty
                cellProductItem = GetPdfCell(orderItem.Quantity, font);
                cellProductItem.HorizontalAlignment = Element.ALIGN_LEFT;
                productsTable.AddCell(cellProductItem);

                //total
                string subTotal;
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    var priceInclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(orderItem.PriceInclTax, order.CurrencyRate);
                    subTotal = _priceFormatter.FormatPrice(priceInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode,
                        lang, true);
                }
                else
                {
                    //excluding tax
                    var priceExclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(orderItem.PriceExclTax, order.CurrencyRate);
                    subTotal = _priceFormatter.FormatPrice(priceExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode,
                        lang, false);
                }
                cellProductItem = GetPdfCell(subTotal, font);
                cellProductItem.HorizontalAlignment = Element.ALIGN_LEFT;
                productsTable.AddCell(cellProductItem);
            }
            doc.Add(productsTable);
        }

        /// <summary>
        /// Print addresses
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="lang">Language</param>
        /// <param name="titleFont">Title font</param>
        /// <param name="order">Order</param>
        /// <param name="font">Text font</param>
        /// <param name="doc">Document</param>
        protected virtual void PrintAddresses(int vendorId, Language lang, Font titleFont, Order order, Font font, Document doc)
        {
            var addressTable = new PdfPTable(2) {RunDirection = GetDirection(lang)};
            addressTable.DefaultCell.Border = Rectangle.NO_BORDER;
            addressTable.WidthPercentage = 100f;
            addressTable.SetWidths(new[] { 50, 50 });

            //billing info
            PrintBillingInfo(vendorId, lang, titleFont, order, font, addressTable);

            //shipping info
            PrintShippingInfo(lang, order, titleFont, font, addressTable);

            doc.Add(addressTable);
            doc.Add(new Paragraph(" "));
        }

        /// <summary>
        /// Print shipping info
        /// </summary>
        /// <param name="lang">Language</param>
        /// <param name="order">Order</param>
        /// <param name="titleFont">Title font</param>
        /// <param name="font">Text font</param>
        /// <param name="addressTable">PDF table for address</param>
        protected virtual void PrintShippingInfo(Language lang, Order order, Font titleFont, Font font, PdfPTable addressTable)
        {
            var shippingAddress = new PdfPTable(1)
            {
                RunDirection = GetDirection(lang)
            };
            shippingAddress.DefaultCell.Border = Rectangle.NO_BORDER;

            if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                //cell = new PdfPCell();
                //cell.Border = Rectangle.NO_BORDER;
                const string indent = "   ";

                if (!order.PickUpInStore)
                {
                    if (order.ShippingAddress == null)
                        throw new NopException($"Shipping is required, but address is not available. Order ID = {order.Id}");
                    
                    shippingAddress.AddCell(GetParagraph("PDFInvoice.ShippingInformation", lang, titleFont));
                    if (!string.IsNullOrEmpty(order.ShippingAddress.Company))
                        shippingAddress.AddCell(GetParagraph("PDFInvoice.Company", indent, lang, font, order.ShippingAddress.Company));
                    shippingAddress.AddCell(GetParagraph("PDFInvoice.Name", indent, lang, font, order.ShippingAddress.FirstName + " " + order.ShippingAddress.LastName));
                    if (_addressSettings.PhoneEnabled)
                        shippingAddress.AddCell(GetParagraph("PDFInvoice.Phone", indent, lang, font, order.ShippingAddress.PhoneNumber));
                    if (_addressSettings.FaxEnabled && !string.IsNullOrEmpty(order.ShippingAddress.FaxNumber))
                        shippingAddress.AddCell(GetParagraph("PDFInvoice.Fax", indent, lang, font, order.ShippingAddress.FaxNumber));
                    if (_addressSettings.StreetAddressEnabled)
                        shippingAddress.AddCell(GetParagraph("PDFInvoice.Address", indent, lang, font, order.ShippingAddress.Address1));
                    if (_addressSettings.StreetAddress2Enabled && !string.IsNullOrEmpty(order.ShippingAddress.Address2))
                        shippingAddress.AddCell(GetParagraph("PDFInvoice.Address2", indent, lang, font, order.ShippingAddress.Address2));
                    if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled ||
                        _addressSettings.ZipPostalCodeEnabled)
                        shippingAddress.AddCell(new Paragraph(
                            $"{indent}{order.ShippingAddress.City}, {(order.ShippingAddress.StateProvince != null ? order.ShippingAddress.StateProvince.GetLocalized(x => x.Name, lang.Id) : "")} {order.ShippingAddress.ZipPostalCode}",
                            font));
                    if (_addressSettings.CountryEnabled && order.ShippingAddress.Country != null)
                        shippingAddress.AddCell(
                            new Paragraph(indent + order.ShippingAddress.Country.GetLocalized(x => x.Name, lang.Id), font));
                    //custom attributes
                    var customShippingAddressAttributes =
                        _addressAttributeFormatter.FormatAttributes(order.ShippingAddress.CustomAttributes);
                    if (!string.IsNullOrEmpty(customShippingAddressAttributes))
                    {
                        //TODO: we should add padding to each line (in case if we have several custom address attributes)
                        shippingAddress.AddCell(new Paragraph(
                            indent + HtmlHelper.ConvertHtmlToPlainText(customShippingAddressAttributes, true, true), font));
                    }
                    shippingAddress.AddCell(new Paragraph(" "));
                }
                else if (order.PickupAddress != null)
                {
                    shippingAddress.AddCell(GetParagraph("PDFInvoice.Pickup", lang, titleFont));
                    if (!string.IsNullOrEmpty(order.PickupAddress.Address1))
                        shippingAddress.AddCell(new Paragraph(
                            $"{indent}{string.Format(_localizationService.GetResource("PDFInvoice.Address", lang.Id), order.PickupAddress.Address1)}",
                            font));
                    if (!string.IsNullOrEmpty(order.PickupAddress.City))
                        shippingAddress.AddCell(new Paragraph($"{indent}{order.PickupAddress.City}", font));
                    if (order.PickupAddress.Country != null)
                        shippingAddress.AddCell(
                            new Paragraph($"{indent}{order.PickupAddress.Country.GetLocalized(x => x.Name, lang.Id)}", font));
                    if (!string.IsNullOrEmpty(order.PickupAddress.ZipPostalCode))
                        shippingAddress.AddCell(new Paragraph($"{indent}{order.PickupAddress.ZipPostalCode}", font));
                    shippingAddress.AddCell(new Paragraph(" "));
                }
                shippingAddress.AddCell(GetParagraph("PDFInvoice.ShippingMethod", indent, lang, font, order.ShippingMethod));
                shippingAddress.AddCell(new Paragraph());

                addressTable.AddCell(shippingAddress);
            }
            else
            {
                shippingAddress.AddCell(new Paragraph());
                addressTable.AddCell(shippingAddress);
            }
        }

        /// <summary>
        /// Print billing info
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="lang">Language</param>
        /// <param name="titleFont">Title font</param>
        /// <param name="order">Order</param>
        /// <param name="font">Text font</param>
        /// <param name="addressTable">Address PDF table</param>
        protected virtual void PrintBillingInfo(int vendorId, Language lang, Font titleFont, Order order, Font font, PdfPTable addressTable)
        {
            const string indent = "   ";
            var billingAddress = new PdfPTable(1) {RunDirection = GetDirection(lang)};
            billingAddress.DefaultCell.Border = Rectangle.NO_BORDER;

            billingAddress.AddCell(GetParagraph("PDFInvoice.BillingInformation", lang, titleFont));

            if (_addressSettings.CompanyEnabled && !string.IsNullOrEmpty(order.BillingAddress.Company))
                billingAddress.AddCell(GetParagraph("PDFInvoice.Company", indent, lang, font, order.BillingAddress.Company));

            billingAddress.AddCell(GetParagraph("PDFInvoice.Name", indent, lang, font, order.BillingAddress.FirstName + " " + order.BillingAddress.LastName));
            if (_addressSettings.PhoneEnabled)
                billingAddress.AddCell(GetParagraph("PDFInvoice.Phone", indent, lang, font, order.BillingAddress.PhoneNumber));
            if (_addressSettings.FaxEnabled && !string.IsNullOrEmpty(order.BillingAddress.FaxNumber))
                billingAddress.AddCell(GetParagraph("PDFInvoice.Fax", indent, lang, font, order.BillingAddress.FaxNumber));
            if (_addressSettings.StreetAddressEnabled)
                billingAddress.AddCell(GetParagraph("PDFInvoice.Address", indent, lang, font, order.BillingAddress.Address1));
            if (_addressSettings.StreetAddress2Enabled && !string.IsNullOrEmpty(order.BillingAddress.Address2))
                billingAddress.AddCell(GetParagraph("PDFInvoice.Address2", indent, lang, font, order.BillingAddress.Address2));
            if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled || _addressSettings.ZipPostalCodeEnabled)
                billingAddress.AddCell(new Paragraph(
                    $"{indent}{order.BillingAddress.City}, {(order.BillingAddress.StateProvince != null ? order.BillingAddress.StateProvince.GetLocalized(x => x.Name, lang.Id) : "")} {order.BillingAddress.ZipPostalCode}",
                    font));
            if (_addressSettings.CountryEnabled && order.BillingAddress.Country != null)
                billingAddress.AddCell(new Paragraph(indent + order.BillingAddress.Country.GetLocalized(x => x.Name, lang.Id),
                    font));

            //VAT number
            if (!string.IsNullOrEmpty(order.VatNumber))
                billingAddress.AddCell(GetParagraph("PDFInvoice.VATNumber", indent, lang, font, order.VatNumber));

            //custom attributes
            var customBillingAddressAttributes =
                _addressAttributeFormatter.FormatAttributes(order.BillingAddress.CustomAttributes);
            if (!string.IsNullOrEmpty(customBillingAddressAttributes))
            {
                //TODO: we should add padding to each line (in case if we have several custom address attributes)
                billingAddress.AddCell(
                    new Paragraph(indent + HtmlHelper.ConvertHtmlToPlainText(customBillingAddressAttributes, true, true), font));
            }

            //vendors payment details
            if (vendorId == 0)
            {
                //payment method
                var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(order.PaymentMethodSystemName);
                var paymentMethodStr = paymentMethod != null
                    ? paymentMethod.GetLocalizedFriendlyName(_localizationService, lang.Id)
                    : order.PaymentMethodSystemName;
                if (!string.IsNullOrEmpty(paymentMethodStr))
                {
                    billingAddress.AddCell(new Paragraph(" "));
                    billingAddress.AddCell(GetParagraph("PDFInvoice.PaymentMethod", indent, lang, font, paymentMethodStr));
                    billingAddress.AddCell(new Paragraph());
                }

                //custom values
                var customValues = order.DeserializeCustomValues();
                if (customValues != null)
                {
                    foreach (var item in customValues)
                    {
                        billingAddress.AddCell(new Paragraph(" "));
                        billingAddress.AddCell(new Paragraph(indent + item.Key + ": " + item.Value, font));
                        billingAddress.AddCell(new Paragraph());
                    }
                }
            }
            addressTable.AddCell(billingAddress);
        }

        /// <summary>
        /// Print header
        /// </summary>
        /// <param name="pdfSettingsByStore">PDF settings</param>
        /// <param name="lang">Language</param>
        /// <param name="order">Order</param>
        /// <param name="font">Text font</param>
        /// <param name="titleFont">Title font</param>
        /// <param name="doc">Document</param>
        protected virtual void PrintHeader(PdfSettings pdfSettingsByStore, Language lang, Order order, Font font, Font titleFont, Document doc)
        {
            //logo
            var logoPicture = _pictureService.GetPictureById(pdfSettingsByStore.LogoPictureId);
            var logoExists = logoPicture != null;

            //header
            var headerTable = new PdfPTable(logoExists ? 2 : 1)
            {
                RunDirection = GetDirection(lang)
            };
            headerTable.DefaultCell.Border = Rectangle.NO_BORDER;

            //store info
            var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;
            var anchor = new Anchor(store.Url.Trim('/'), font)
            {
                Reference = store.Url
            };

            var cellHeader = GetPdfCell(string.Format(_localizationService.GetResource("PDFInvoice.Order#", lang.Id), order.CustomOrderNumber), titleFont);
            cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
            cellHeader.Phrase.Add(new Phrase(anchor));
            cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
            cellHeader.Phrase.Add(GetParagraph("PDFInvoice.OrderDate", lang, font, _dateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc).ToString("D", new CultureInfo(lang.LanguageCulture))));
            cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
            cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
            cellHeader.HorizontalAlignment = Element.ALIGN_LEFT;
            cellHeader.Border = Rectangle.NO_BORDER;

            headerTable.AddCell(cellHeader);

            if (logoExists)
                headerTable.SetWidths(lang.Rtl ? new[] {0.2f, 0.8f} : new[] {0.8f, 0.2f});
            headerTable.WidthPercentage = 100f;

            //logo               
            if (logoExists)
            {
                var logoFilePath = _pictureService.GetThumbLocalPath(logoPicture, 0, false);
                var logo = Image.GetInstance(logoFilePath);
                logo.Alignment = GetAlignment(lang, true);
                logo.ScaleToFit(65f, 65f);

                var cellLogo = new PdfPCell {Border = Rectangle.NO_BORDER};
                cellLogo.AddElement(logo);
                headerTable.AddCell(cellLogo);
            }
            doc.Add(headerTable);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Print an order to PDF
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        /// <param name="vendorId">Vendor identifier to limit products; 0 to to print all products. If specified, then totals won't be printed</param>
        /// <returns>A path of generated file</returns>
        public virtual string PrintOrderToPdf(Order order, int languageId = 0, int vendorId = 0)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var fileName = $"order_{order.OrderGuid}_{CommonHelper.GenerateRandomDigitCode(4)}.pdf";
            var filePath = Path.Combine(CommonHelper.MapPath("~/wwwroot/files/exportimport"), fileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var orders = new List<Order> {order};
                PrintOrdersToPdf(fileStream, orders, languageId, vendorId);
            }
            return filePath;
        }

        /// <summary>
        /// Print orders to PDF
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="orders">Orders</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        /// <param name="vendorId">Vendor identifier to limit products; 0 to to print all products. If specified, then totals won't be printed</param>
        public virtual void PrintOrdersToPdf(Stream stream, IList<Order> orders, int languageId = 0, int vendorId = 0)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (orders == null)
                throw new ArgumentNullException(nameof(orders));

            var pageSize = PageSize.A4;

            if (_pdfSettings.LetterPageSizeEnabled)
            {
                pageSize = PageSize.LETTER;
            }

            var doc = new Document(pageSize);
            var pdfWriter = PdfWriter.GetInstance(doc, stream);
            doc.Open();

            //fonts
            var titleFont = GetFont();
            titleFont.SetStyle(Font.BOLD);
            titleFont.Color = BaseColor.BLACK;
            var font = GetFont();
            var attributesFont = GetFont();
            attributesFont.SetStyle(Font.ITALIC);

            var ordCount = orders.Count;
            var ordNum = 0;

            foreach (var order in orders)
            {
                //by default _pdfSettings contains settings for the current active store
                //and we need PdfSettings for the store which was used to place an order
                //so let's load it based on a store of the current order
                var pdfSettingsByStore = _settingService.LoadSetting<PdfSettings>(order.StoreId);

                var lang = _languageService.GetLanguageById(languageId == 0 ? order.CustomerLanguageId : languageId);
                if (lang == null || !lang.Published)
                    lang = _workContext.WorkingLanguage;

                //header
                PrintHeader(pdfSettingsByStore, lang, order, font, titleFont, doc);

                //addresses
                PrintAddresses(vendorId, lang, titleFont, order, font, doc);
                
                //products
                PrintProducts(vendorId, lang, titleFont, doc, order, font, attributesFont);
                
                //checkout attributes
                PrintCheckoutAttributes(vendorId, order, doc, lang, font);

                //totals
                PrintTotals(vendorId, lang, order, font, titleFont, doc);

                //order notes
                PrintOrderNotes(pdfSettingsByStore, order, lang, titleFont, doc, font);

                //footer
                PrintFooter(pdfSettingsByStore, pdfWriter, pageSize, lang, font);

                ordNum++;
                if (ordNum < ordCount)
                {
                    doc.NewPage();
                }
            }
            doc.Close();
        }
        
        /// <summary>
        /// Print packaging slips to PDF
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="shipments">Shipments</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        public virtual void PrintPackagingSlipsToPdf(Stream stream, IList<Shipment> shipments, int languageId = 0)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (shipments == null)
                throw new ArgumentNullException(nameof(shipments));

            var pageSize = PageSize.A4;

            if (_pdfSettings.LetterPageSizeEnabled)
            {
                pageSize = PageSize.LETTER;
            }

            var doc = new Document(pageSize);
            PdfWriter.GetInstance(doc, stream);
            doc.Open();

            //fonts
            var titleFont = GetFont();
            titleFont.SetStyle(Font.BOLD);
            titleFont.Color = BaseColor.BLACK;
            var font = GetFont();
            var attributesFont = GetFont();
            attributesFont.SetStyle(Font.ITALIC);
            
            var shipmentCount = shipments.Count;
            var shipmentNum = 0;

            foreach (var shipment in shipments)
            {
                var order = shipment.Order;

                var lang = _languageService.GetLanguageById(languageId == 0 ? order.CustomerLanguageId : languageId);
                if (lang == null || !lang.Published)
                    lang = _workContext.WorkingLanguage;

                var addressTable = new PdfPTable(1);
                if (lang.Rtl)
                    addressTable.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
                addressTable.DefaultCell.Border = Rectangle.NO_BORDER;
                addressTable.WidthPercentage = 100f;

                addressTable.AddCell(GetParagraph("PDFPackagingSlip.Shipment", lang, titleFont, shipment.Id));
                addressTable.AddCell(GetParagraph("PDFPackagingSlip.Order", lang, titleFont, order.CustomOrderNumber));

                if (!order.PickUpInStore)
                {
                    if (order.ShippingAddress == null)
                        throw new NopException($"Shipping is required, but address is not available. Order ID = {order.Id}");

                    if (_addressSettings.CompanyEnabled && !string.IsNullOrEmpty(order.ShippingAddress.Company))
                        addressTable.AddCell(GetParagraph("PDFPackagingSlip.Company", lang, font, order.ShippingAddress.Company));

                    addressTable.AddCell(GetParagraph("PDFPackagingSlip.Name", lang, font, order.ShippingAddress.FirstName + " " + order.ShippingAddress.LastName));
                    if (_addressSettings.PhoneEnabled)
                        addressTable.AddCell(GetParagraph("PDFPackagingSlip.Phone", lang, font, order.ShippingAddress.PhoneNumber));
                    if (_addressSettings.StreetAddressEnabled)
                        addressTable.AddCell(GetParagraph("PDFPackagingSlip.Address", lang, font, order.ShippingAddress.Address1));

                    if (_addressSettings.StreetAddress2Enabled && !string.IsNullOrEmpty(order.ShippingAddress.Address2))
                        addressTable.AddCell(GetParagraph("PDFPackagingSlip.Address2", lang, font, order.ShippingAddress.Address2));

                    if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled || _addressSettings.ZipPostalCodeEnabled)
                        addressTable.AddCell(new Paragraph($"{order.ShippingAddress.City}, {(order.ShippingAddress.StateProvince != null ? order.ShippingAddress.StateProvince.GetLocalized(x => x.Name, lang.Id) : "")} {order.ShippingAddress.ZipPostalCode}", font));

                    if (_addressSettings.CountryEnabled && order.ShippingAddress.Country != null)
                        addressTable.AddCell(new Paragraph(order.ShippingAddress.Country.GetLocalized(x => x.Name, lang.Id), font));

                    //custom attributes
                    var customShippingAddressAttributes = _addressAttributeFormatter.FormatAttributes(order.ShippingAddress.CustomAttributes);
                    if (!string.IsNullOrEmpty(customShippingAddressAttributes))
                    {
                        addressTable.AddCell(new Paragraph(HtmlHelper.ConvertHtmlToPlainText(customShippingAddressAttributes, true, true), font));
                    }
                }
                else
                    if (order.PickupAddress != null)
                    {
                        addressTable.AddCell(new Paragraph(_localizationService.GetResource("PDFInvoice.Pickup", lang.Id), titleFont));
                        if (!string.IsNullOrEmpty(order.PickupAddress.Address1))
                            addressTable.AddCell(new Paragraph($"   {string.Format(_localizationService.GetResource("PDFInvoice.Address", lang.Id), order.PickupAddress.Address1)}", font));
                        if (!string.IsNullOrEmpty(order.PickupAddress.City))
                            addressTable.AddCell(new Paragraph($"   {order.PickupAddress.City}", font));
                        if (order.PickupAddress.Country != null)
                            addressTable.AddCell(new Paragraph($"   {order.PickupAddress.Country.GetLocalized(x => x.Name, lang.Id)}", font));
                        if (!string.IsNullOrEmpty(order.PickupAddress.ZipPostalCode))
                            addressTable.AddCell(new Paragraph($"   {order.PickupAddress.ZipPostalCode}", font));
                        addressTable.AddCell(new Paragraph(" "));
                    }
                
                addressTable.AddCell(new Paragraph(" "));

                addressTable.AddCell(GetParagraph("PDFPackagingSlip.ShippingMethod", lang, font, order.ShippingMethod));
                addressTable.AddCell(new Paragraph(" "));
                doc.Add(addressTable);

                var productsTable = new PdfPTable(3) {WidthPercentage = 100f};
                if (lang.Rtl)
                {
                    productsTable.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
                    productsTable.SetWidths(new[] {20, 20, 60});
                }
                else
                {
                    productsTable.SetWidths(new[] {60, 20, 20});
                }

                //product name
                var cell = GetPdfCell("PDFPackagingSlip.ProductName", lang, font);
                cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                productsTable.AddCell(cell);

                //SKU
                cell = GetPdfCell("PDFPackagingSlip.SKU", lang, font);
                cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                productsTable.AddCell(cell);

                //qty
                cell = GetPdfCell("PDFPackagingSlip.QTY", lang, font);
                cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                productsTable.AddCell(cell);

                foreach (var si in shipment.ShipmentItems)
                {
                    var productAttribTable = new PdfPTable(1);
                    if (lang.Rtl)
                        productAttribTable.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
                    productAttribTable.DefaultCell.Border = Rectangle.NO_BORDER;

                    //product name
                    var orderItem = _orderService.GetOrderItemById(si.OrderItemId);
                    if (orderItem == null)
                        continue;

                    var p = orderItem.Product;
                    var name = p.GetLocalized(x => x.Name, lang.Id);
                    productAttribTable.AddCell(new Paragraph(name, font));
                    //attributes
                    if (!string.IsNullOrEmpty(orderItem.AttributeDescription))
                    {
                        var attributesParagraph = new Paragraph(HtmlHelper.ConvertHtmlToPlainText(orderItem.AttributeDescription, true, true), attributesFont);
                        productAttribTable.AddCell(attributesParagraph);
                    }
                    //rental info
                    if (orderItem.Product.IsRental)
                    {
                        var rentalStartDate = orderItem.RentalStartDateUtc.HasValue ? orderItem.Product.FormatRentalDate(orderItem.RentalStartDateUtc.Value) : "";
                        var rentalEndDate = orderItem.RentalEndDateUtc.HasValue ? orderItem.Product.FormatRentalDate(orderItem.RentalEndDateUtc.Value) : "";
                        var rentalInfo = string.Format(_localizationService.GetResource("Order.Rental.FormattedDate"),
                            rentalStartDate, rentalEndDate);

                        var rentalInfoParagraph = new Paragraph(rentalInfo, attributesFont);
                        productAttribTable.AddCell(rentalInfoParagraph);
                    }
                    productsTable.AddCell(productAttribTable);

                    //SKU
                    var sku = p.FormatSku(orderItem.AttributesXml, _productAttributeParser);
                    cell = GetPdfCell(sku ?? string.Empty, font);
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    productsTable.AddCell(cell);

                    //qty
                    cell = GetPdfCell(si.Quantity, font);
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    productsTable.AddCell(cell);
                }
                doc.Add(productsTable);

                shipmentNum++;
                if (shipmentNum < shipmentCount)
                {
                    doc.NewPage();
                }
            }

            doc.Close();
        }

        /// <summary>
        /// Print products to PDF
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="products">Products</param>
        public virtual void PrintProductsToPdf(Stream stream, IList<Product> products)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (products == null)
                throw new ArgumentNullException(nameof(products));

            var lang = _workContext.WorkingLanguage;

            var pageSize = PageSize.A4;

            if (_pdfSettings.LetterPageSizeEnabled)
            {
                pageSize = PageSize.LETTER;
            }

            var doc = new Document(pageSize);
            PdfWriter.GetInstance(doc, stream);
            doc.Open();

            //fonts
            var titleFont = GetFont();
            titleFont.SetStyle(Font.BOLD);
            titleFont.Color = BaseColor.BLACK;
            var font = GetFont();

            var productNumber = 1;
            var prodCount = products.Count;

            foreach (var product in products)
            {
                var productName = product.GetLocalized(x => x.Name, lang.Id);
                var productDescription = product.GetLocalized(x => x.FullDescription, lang.Id);

                var productTable = new PdfPTable(1) {WidthPercentage = 100f};
                productTable.DefaultCell.Border = Rectangle.NO_BORDER;
                if (lang.Rtl)
                {
                    productTable.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
                }

                productTable.AddCell(new Paragraph($"{productNumber}. {productName}", titleFont));
                productTable.AddCell(new Paragraph(" "));
                productTable.AddCell(new Paragraph(HtmlHelper.StripTags(HtmlHelper.ConvertHtmlToPlainText(productDescription, decode: true)), font));
                productTable.AddCell(new Paragraph(" "));

                if (product.ProductType == ProductType.SimpleProduct)
                {
                    //simple product
                    //render its properties such as price, weight, etc
                    var priceStr = $"{product.Price:0.00} {_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode}";
                    if (product.IsRental)
                        priceStr = _priceFormatter.FormatRentalProductPeriod(product, priceStr);
                    productTable.AddCell(new Paragraph($"{_localizationService.GetResource("PDFProductCatalog.Price", lang.Id)}: {priceStr}", font));
                    productTable.AddCell(new Paragraph($"{_localizationService.GetResource("PDFProductCatalog.SKU", lang.Id)}: {product.Sku}", font));

                    if (product.IsShipEnabled && product.Weight > decimal.Zero)
                        productTable.AddCell(new Paragraph($"{_localizationService.GetResource("PDFProductCatalog.Weight", lang.Id)}: {product.Weight:0.00} {_measureService.GetMeasureWeightById(_measureSettings.BaseWeightId).Name}", font));

                    if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock)
                        productTable.AddCell(new Paragraph($"{_localizationService.GetResource("PDFProductCatalog.StockQuantity", lang.Id)}: {product.GetTotalStockQuantity()}", font));

                    productTable.AddCell(new Paragraph(" "));
                }
                var pictures = _pictureService.GetPicturesByProductId(product.Id);
                if (pictures.Any())
                {
                    var table = new PdfPTable(2) {WidthPercentage = 100f};
                    if (lang.Rtl)
                    {
                        table.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
                    }

                    foreach (var pic in pictures)
                    {
                        var picBinary = _pictureService.LoadPictureBinary(pic);
                        if (picBinary == null || picBinary.Length <= 0)
                            continue;

                        var pictureLocalPath = _pictureService.GetThumbLocalPath(pic, 200, false);
                        var cell = new PdfPCell(Image.GetInstance(pictureLocalPath))
                        {
                            HorizontalAlignment = Element.ALIGN_LEFT,
                            Border = Rectangle.NO_BORDER
                        };
                        table.AddCell(cell);
                    }

                    if (pictures.Count % 2 > 0)
                    {
                        var cell = new PdfPCell(new Phrase(" "))
                        {
                            Border = Rectangle.NO_BORDER
                        };
                        table.AddCell(cell);
                    }

                    productTable.AddCell(table);
                    productTable.AddCell(new Paragraph(" "));
                }

                if (product.ProductType == ProductType.GroupedProduct)
                {
                    //grouped product. render its associated products
                    var pvNum = 1;
                    foreach (var associatedProduct in _productService.GetAssociatedProducts(product.Id, showHidden: true))
                    {
                        productTable.AddCell(new Paragraph($"{productNumber}-{pvNum}. {associatedProduct.GetLocalized(x => x.Name, lang.Id)}", font));
                        productTable.AddCell(new Paragraph(" "));

                        //uncomment to render associated product description
                        //string apDescription = associatedProduct.GetLocalized(x => x.ShortDescription, lang.Id);
                        //if (!string.IsNullOrEmpty(apDescription))
                        //{
                        //    productTable.AddCell(new Paragraph(HtmlHelper.StripTags(HtmlHelper.ConvertHtmlToPlainText(apDescription)), font));
                        //    productTable.AddCell(new Paragraph(" "));
                        //}

                        //uncomment to render associated product picture
                        //var apPicture = _pictureService.GetPicturesByProductId(associatedProduct.Id).FirstOrDefault();
                        //if (apPicture != null)
                        //{
                        //    var picBinary = _pictureService.LoadPictureBinary(apPicture);
                        //    if (picBinary != null && picBinary.Length > 0)
                        //    {
                        //        var pictureLocalPath = _pictureService.GetThumbLocalPath(apPicture, 200, false);
                        //        productTable.AddCell(Image.GetInstance(pictureLocalPath));
                        //    }
                        //}

                        productTable.AddCell(new Paragraph($"{_localizationService.GetResource("PDFProductCatalog.Price", lang.Id)}: {associatedProduct.Price:0.00} {_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode}", font));
                        productTable.AddCell(new Paragraph($"{_localizationService.GetResource("PDFProductCatalog.SKU", lang.Id)}: {associatedProduct.Sku}", font));

                        if (associatedProduct.IsShipEnabled && associatedProduct.Weight > decimal.Zero)
                            productTable.AddCell(new Paragraph($"{_localizationService.GetResource("PDFProductCatalog.Weight", lang.Id)}: {associatedProduct.Weight:0.00} {_measureService.GetMeasureWeightById(_measureSettings.BaseWeightId).Name}", font));

                        if (associatedProduct.ManageInventoryMethod == ManageInventoryMethod.ManageStock)
                            productTable.AddCell(new Paragraph($"{_localizationService.GetResource("PDFProductCatalog.StockQuantity", lang.Id)}: {associatedProduct.GetTotalStockQuantity()}", font));

                        productTable.AddCell(new Paragraph(" "));

                        pvNum++;
                    }
                }

                doc.Add(productTable);

                productNumber++;

                if (productNumber <= prodCount)
                {
                    doc.NewPage();
                }
            }

            doc.Close();
        }

        #endregion
    }
}