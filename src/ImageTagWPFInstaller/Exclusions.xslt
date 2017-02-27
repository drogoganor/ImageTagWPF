<?xml version="1.0" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">

  <!-- Copy all attributes and elements to the output. -->
  <xsl:template match="@*|*">
    <xsl:copy>
      <xsl:apply-templates select="@*" />
      <xsl:apply-templates select="*" />
    </xsl:copy>
  </xsl:template>

  <xsl:output method="xml" indent="yes" />
  
  <xsl:key name="ignored-components-search" match="wix:Component[contains(wix:File/@Source, 'imagetag.db')
           or contains(wix:File/@Source, '.vshost.exe')
           or contains(wix:File/@Source, '.txt')
           or contains(wix:File/@Source, '.pdb')
           or contains(wix:File/@Source, '.xml')]" use="@Id" />
  
  <xsl:template match="wix:ComponentRef[key('ignored-components-search', @Id)]"/>
  <xsl:template match="wix:Component[key('ignored-components-search', @Id)]"/>
  
  
</xsl:stylesheet>